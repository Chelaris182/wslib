using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using wslib.Protocol.Reader;
using wslib.Protocol.Writer;
using wslib.Utils;

namespace wslib.Protocol
{
    public class WebSocket : IWebSocket
    {
        public bool IsConnected() => stream.CanRead;
        public Dictionary<string, object> Env;

        private readonly Stream stream;
        private readonly List<IMessageExtension> extensions;
        private readonly bool serverSocket;
        private readonly SemaphoreSlim writeSemaphore = new SemaphoreSlim(1, 1);
        private int isClosing;
        internal DateTime LastActivity = DateTime.Now;

        public WebSocket(Dictionary<string, object> env, Stream stream, List<IMessageExtension> extensions, bool serverSocket)
        {
            Env = env;
            this.stream = stream;
            this.extensions = extensions;
            this.serverSocket = serverSocket;
        }

        private void refreshActivity()
        {
            LastActivity = DateTime.Now;
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
                WsFrame frame = await ReadFrameAsync(cancellationToken).ConfigureAwait(false);
                if (frame == null) return null;

                if (frame.Header.OPCODE == WsFrameHeader.Opcodes.CONTINUATION) throw new ProtocolViolationException("unexpected frame type: " + frame.Header.OPCODE);
                var messageType = frame.Header.OPCODE == WsFrameHeader.Opcodes.TEXT ? MessageType.Text : MessageType.Binary;
                var wsMesageReadStream = new WsMesageReader(this, frame);
                Stream payloadStream = new WsReadStream(wsMesageReadStream);
                if (extensions != null)
                {
                    payloadStream = extensions.Aggregate(payloadStream, (current, extension) => extension.ApplyRead(current, frame));
                }

                return new WsMessage(messageType, payloadStream);
            }
            catch (IOException e) // happens when read or write returns error
            {
                // TODO: log?
            }
            catch (ProtocolViolationException e)
            {
                if (IsConnected())
                {
                    await CloseAsync(CloseStatusCode.ProtocolError, cancellationToken).ConfigureAwait(false); // TODO: may throw exception?
                }

                throw;
            }
            catch (InvalidOperationException e) // happens when read or write happens on a closed socket
            {
                // TODO: log?
            }

            return null;
        }

        internal async Task<WsFrame> ReadFrameAsync(CancellationToken cancellationToken)
        {
            while (IsConnected())
            {
                var frame = await WsDissector.ReadFrameHeader(stream, serverSocket, cancellationToken).ConfigureAwait(false);
                if (!isDataFrame(frame))
                {
                    await processControlFrame(frame, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                return frame;
            }

            return null;
        }

        internal async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var r = await stream.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
            if (r > 0) refreshActivity();
            return r;
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
                await CloseAsync(CloseStatusCode.MessageTooLarge, cancellationToken).ConfigureAwait(false);
                throw new ProtocolViolationException("control frame is too large");
            }

            if (!frame.Header.FIN)
            {
                // current code doesn't support multi-frame control messages
                await CloseAsync(CloseStatusCode.UnexpectedCondition, cancellationToken).ConfigureAwait(false);
                throw new ProtocolViolationException("multi-frame control frames aren't supported");
            }

            byte[] payload = new byte[frame.PayloadLength];
            await stream.ReadUntil(payload, 0, payload.Length, cancellationToken).ConfigureAwait(false);

            switch (frame.Header.OPCODE)
            {
                case WsFrameHeader.Opcodes.CLOSE:
                    if (payload.Length >= 2)
                        await closeAsync(payload, cancellationToken).ConfigureAwait(false);
                    else
                        await CloseAsync(CloseStatusCode.NormalClosure, cancellationToken).ConfigureAwait(false);
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

        public Task CloseAsync(CloseStatusCode statusCode, CancellationToken cancellationToken)
        {
            var s = (short)statusCode;
            return closeAsync(new[] { (byte)(s >> 8), (byte)(s & 0xff) }, cancellationToken);
        }

        private async Task closeAsync(byte[] closeCode, CancellationToken cancellationToken)
        {
            if (Interlocked.CompareExchange(ref isClosing, 1, 0) != 0) return;

            try
            {
                await SendMessage(WsFrameHeader.Opcodes.CLOSE, closeCode, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                stream.Close();
            }
        }

        private async Task sendPong(byte[] payload, CancellationToken cancellationToken)
        {
            await SendMessage(WsFrameHeader.Opcodes.PONG, payload, cancellationToken).ConfigureAwait(false);
        }

        private static bool isDataFrame(WsFrame frame)
        {
            return frame.Header.OPCODE == WsFrameHeader.Opcodes.BINARY || frame.Header.OPCODE == WsFrameHeader.Opcodes.TEXT || frame.Header.OPCODE == WsFrameHeader.Opcodes.CONTINUATION;
        }

        public async Task SendMessage(WsFrameHeader.Opcodes opcode, byte[] payload, CancellationToken cancellationToken)
        {
            await writeSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false); // TODO: timeout
            try
            {
                var header = WsDissector.SerializeFrameHeader(new WsFrameHeader(0, 0) { FIN = true, OPCODE = opcode }, payload.Length, null);
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