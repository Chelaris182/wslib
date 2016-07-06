using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using wslib.Protocol.Writer;
using wslib.Utils;

namespace wslib.Protocol
{
    public class WebSocket : IWebSocket
    {
        public bool IsConnected() => stream.CanRead;

        private Dictionary<string, object> env;
        private readonly Stream stream;
        private readonly List<IMessageExtension> extensions;
        private readonly bool serverSocket;
        private readonly SemaphoreSlim writeSemaphore = new SemaphoreSlim(1, 1);
        private DateTime lastActivity;
        private bool isClosing;

        public WebSocket(Dictionary<string, object> env, Stream stream, List<IMessageExtension> extensions, bool serverSocket)
        {
            this.env = env;
            this.stream = stream;
            this.extensions = extensions;
            this.serverSocket = serverSocket;

            runHeartbit();
        }

        private async Task runHeartbit()
        {
            lastActivity = DateTime.Now;

            while (IsConnected())
            {
                var now = DateTime.Now;
                if (lastActivity.Add(TimeSpan.FromSeconds(10)) < now)
                {
                    await closeConnection(CloseStatusCode.ProtocolError, CancellationToken.None).ConfigureAwait(false);
                    return;
                }

                var pingTime = lastActivity.Add(TimeSpan.FromSeconds(5));
                var toSleep = pingTime - now;
                if (pingTime < now)
                {
                    await sendPing(new byte[] { 0x00 }, CancellationToken.None).ConfigureAwait(false);
                    toSleep = TimeSpan.FromSeconds(5);
                }

                await Task.Delay(toSleep).ConfigureAwait(false);
            }
        }

        private void refreshActivity()
        {
            lastActivity = DateTime.Now;
        }

        public void Dispose()
        {
            writeSemaphore.Dispose();
            stream.Dispose();
        }

        public async Task<WsMessage> ReadMessageAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (IsConnected())
                {
                    var frame = await WsDissector.ReadFrameHeader(stream, serverSocket, cancellationToken).ConfigureAwait(false);
                    if (!isDataFrame(frame))
                    {
                        await processControlFrame(frame, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    var messageType = frame.Header.OPCODE == WsFrameHeader.Opcodes.TEXT ? MessageType.Text : MessageType.Binary;
                    Stream payloadStream = new WsMesageReadStream(frame, stream, refreshActivity);
                    if (extensions != null)
                    {
                        payloadStream = extensions.Aggregate(payloadStream, (current, extension) => extension.ApplyRead(current, frame));
                    }

                    return new WsMessage(messageType, payloadStream);
                }
            }
            catch (IOException e) // happens when read or write returns error
            {
                stream.Close(); // TODO: log
            }
            catch (ProtocolViolationException e)
            {
                await closeConnection(CloseStatusCode.ProtocolError, cancellationToken).ConfigureAwait(false); // TODO: may throw exception?
            }
            catch (InvalidOperationException e) // happens when read or write happens on a closed socket
            {
                // TODO: log?
            }

            return null;
        }

        public async Task<WsMessageWriter> CreateMessageWriter(MessageType type, CancellationToken cancellationToken)
        {
            await writeSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            IWsMessageWriteStream s = new WsWireStream(stream);
            s = extensions.Aggregate(s, (current, extension) => extension.ApplyWrite(current));
            return new WsMessageWriter(type, () => writeSemaphore.Release(), s); // TODO replace action with disposable object
        }

        private async Task processControlFrame(WsFrame frame, CancellationToken cancellationToken)
        {
            if (frame.PayloadLength > 1024)
            {
                await closeConnection(CloseStatusCode.MessageTooLarge, cancellationToken).ConfigureAwait(false);
                return;
            }

            if (!frame.Header.FIN)
            {
                // current code doesn't support multi-frame control messages
                await closeConnection(CloseStatusCode.UnexpectedCondition, cancellationToken).ConfigureAwait(false);
                return;
            }

            byte[] payload = new byte[frame.PayloadLength];
            await stream.ReadUntil(payload, 0, payload.Length, cancellationToken).ConfigureAwait(false);
            refreshActivity();

            switch (frame.Header.OPCODE)
            {
                case WsFrameHeader.Opcodes.CLOSE:
                    if (isClosing) return;

                    if (payload.Length >= 2)
                        await closeConnection(payload, cancellationToken).ConfigureAwait(false);
                    else
                        await closeConnection(CloseStatusCode.NormalClosure, cancellationToken).ConfigureAwait(false);
                    return;

                case WsFrameHeader.Opcodes.PING:
                    sendPong(payload, cancellationToken); // fire&forget
                    break;

                case WsFrameHeader.Opcodes.PONG: // do nothing
                    break;

                default:
                    // TODO: extensions may define additional opcode
                    throw new ProtocolViolationException("Unexpected frame type");
            }
        }

        private Task closeConnection(CloseStatusCode statusCode, CancellationToken cancellationToken)
        {
            var s = (short)statusCode;
            return closeConnection(new[] { (byte)(s >> 8), (byte)(s & 0xff) }, cancellationToken);
        }

        private async Task closeConnection(byte[] closeCode, CancellationToken cancellationToken)
        {
            isClosing = true;
            try
            {
                await sendControlMessage(WsFrameHeader.Opcodes.CLOSE, closeCode, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                stream.Close();
            }
        }

        private async Task sendPing(byte[] payload, CancellationToken cancellationToken)
        {
            await sendControlMessage(WsFrameHeader.Opcodes.PING, payload, cancellationToken).ConfigureAwait(false);
        }

        private async Task sendPong(byte[] payload, CancellationToken cancellationToken)
        {
            await sendControlMessage(WsFrameHeader.Opcodes.PONG, payload, cancellationToken).ConfigureAwait(false);
        }

        private static bool isDataFrame(WsFrame frame)
        {
            return frame.Header.OPCODE == WsFrameHeader.Opcodes.BINARY || frame.Header.OPCODE == WsFrameHeader.Opcodes.TEXT;
        }

        private async Task sendControlMessage(WsFrameHeader.Opcodes opcode, byte[] payload, CancellationToken cancellationToken)
        {
            await writeSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false); // TODO: timeout
            try
            {
                var header = WsDissector.SerializeFrameHeader(new WsFrameHeader(0, 0) { FIN = true, OPCODE = opcode }, payload.Length);
                await stream.WriteAsync(header.Array, header.Offset, header.Count, cancellationToken).ConfigureAwait(false);
                await stream.WriteAsync(payload, 0, payload.Length, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                writeSemaphore.Release(); // TODO: replace with disposable object
            }
        }
    }
}