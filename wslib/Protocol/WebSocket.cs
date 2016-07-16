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
        public readonly Dictionary<string, object> Env;
        private readonly Stream stream;
        private readonly List<IMessageExtension> extensions;
        private readonly bool serverSocket;
        private readonly SemaphoreSlim writeSemaphore = new SemaphoreSlim(1, 1);
        private readonly ArraySegment<byte> headerBuffer;
        private readonly ArraySegment<byte> payloadBuffer;

        private int isClosing;
        internal DateTime LastActivity = DateTime.Now;
        public bool IsConnected() => stream.CanRead;

        public WebSocket(Dictionary<string, object> env, Stream stream, List<IMessageExtension> extensions, bool serverSocket)
        {
            Env = env;
            this.stream = stream;
            this.extensions = extensions;
            this.serverSocket = serverSocket;
            var receiveBuffer = new byte[128];
            headerBuffer = new ArraySegment<byte>(receiveBuffer, 0, 14);
            payloadBuffer = new ArraySegment<byte>(receiveBuffer, 14, receiveBuffer.Length - 14);
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

                MessageType messageType;
                switch (frame.Header.OPCODE)
                {
                    case WsFrameHeader.Opcodes.TEXT:
                        messageType = MessageType.Text;
                        break;
                    case WsFrameHeader.Opcodes.BINARY:
                        messageType = MessageType.Binary;
                        break;
                    default:
                        throw new ProtocolViolationException("unexpected frame type: " + frame.Header.OPCODE);
                }

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
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var frame = await WsDissector.ReadFrameHeader(stream, headerBuffer, serverSocket, cancellationToken).ConfigureAwait(false);
                if (isControlFrame(frame))
                {
                    await processControlFrame(frame, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                return frame;
            }
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
            if (frame.PayloadLength > (ulong)payloadBuffer.Count)
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

            int payloadLen = (int)frame.PayloadLength;
            await stream.ReadUntil(payloadBuffer, 0, payloadLen, cancellationToken).ConfigureAwait(false);
            var payload = new ArraySegment<byte>(payloadBuffer.Array, payloadBuffer.Offset, payloadLen);

            switch (frame.Header.OPCODE)
            {
                case WsFrameHeader.Opcodes.CLOSE:
                    if (payloadLen >= 2)
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
            var payload = new ArraySegment<byte>(new[] { (byte)(s >> 8), (byte)(s & 0xff) });
            return closeAsync(payload, cancellationToken);
        }

        private async Task closeAsync(ArraySegment<byte> payload, CancellationToken cancellationToken)
        {
            if (Interlocked.CompareExchange(ref isClosing, 1, 0) != 0) return;

            try
            {
                await SendMessage(WsFrameHeader.Opcodes.CLOSE, payload, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                stream.Close();
            }
        }

        private Task sendPong(ArraySegment<byte> payload, CancellationToken cancellationToken)
        {
            return SendMessage(WsFrameHeader.Opcodes.PONG, payload, cancellationToken);
        }

        private static bool isControlFrame(WsFrame frame)
        {
            return frame.Header.OPCODE >= WsFrameHeader.Opcodes.CLOSE;
        }

        public async Task SendMessage(WsFrameHeader.Opcodes opcode, ArraySegment<byte> payload, CancellationToken cancellationToken)
        {
            await writeSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var header = WsDissector.SerializeFrameHeader(new WsFrameHeader { FIN = true, OPCODE = opcode }, payload.Count, null);
                await stream.WriteAsync(header, cancellationToken).ConfigureAwait(false);
                await stream.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                writeSemaphore.Release(); // TODO: replace with disposable object
            }
        }
    }
}