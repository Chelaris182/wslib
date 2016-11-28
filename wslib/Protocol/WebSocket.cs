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
        private const int maxControlPayload = 125;
        private const int maxFrameHeaderLength = 14;

        public readonly Dictionary<string, object> Env;
        private readonly Stream stream;
        private readonly List<IMessageExtension> extensions;
        private readonly bool serverSocket;
        private readonly SemaphoreSlim writeSemaphore = new SemaphoreSlim(1, 1);
        private readonly ArraySegment<byte> headerBuffer;
        private readonly ArraySegment<byte> payloadBuffer;
        private DateTime lastActivity = DateTime.Now;
        private int state = (int)State.OPENED;

        private enum State
        {
            OPENED,
            CLOSE_RECEIVED,
            SENDING_CLOSE,
            CLOSE_SENT,
            CLOSED
        }

        public bool IsConnected() => stream.CanRead && stream.CanWrite;

        public DateTime LastActivity()
        {
            return lastActivity;
        }

        public WebSocket(Dictionary<string, object> env, Stream stream, List<IMessageExtension> extensions, bool serverSocket)
        {
            Env = env;
            this.stream = stream;
            this.extensions = extensions;
            this.serverSocket = serverSocket;
            var receiveBuffer = new byte[maxFrameHeaderLength + maxControlPayload];
            headerBuffer = new ArraySegment<byte>(receiveBuffer, 0, maxFrameHeaderLength);
            payloadBuffer = new ArraySegment<byte>(receiveBuffer, maxFrameHeaderLength, receiveBuffer.Length - maxFrameHeaderLength);
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
                await cleanClose().ConfigureAwait(false);
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
                await cleanClose().ConfigureAwait(false);
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
            if (r > 0) lastActivity = DateTime.Now;
            return r;
        }

        public async Task<WsMessageWriter> CreateMessageWriter(MessageType type, CancellationToken cancellationToken)
        {
            await writeSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            IWsMessageWriteStream s = new WsWireStream(stream);
            if (extensions != null)
            {
                s = extensions.Aggregate(s, (current, extension) => extension.ApplyWrite(current));
            }
            return new WsMessageWriter(type, () => writeSemaphore.Release(), s); // TODO replace action with disposable object
        }

        private async Task processControlFrame(WsFrame frame, CancellationToken cancellationToken)
        {
            if (frame.PayloadLength > maxControlPayload)
            {
                // control frames MUST have a payload length of 125 bytes or less
                await CloseAsync(CloseStatusCode.ProtocolError, cancellationToken).ConfigureAwait(false);
                return;
            }

            if (!frame.Header.FIN)
            {
                // control frames MUST NOT be fragmented                                                
                await CloseAsync(CloseStatusCode.ProtocolError, cancellationToken).ConfigureAwait(false);
                return;
            }

            var wsMesageReadStream = new WsMesageReader(this, frame);
            Stream payloadStream = new WsReadStream(wsMesageReadStream);
            await payloadStream.ReadUntil(payloadBuffer, 0, (int)frame.PayloadLength, cancellationToken).ConfigureAwait(false);
            var payload = new ArraySegment<byte>(payloadBuffer.Array, payloadBuffer.Offset, (int)frame.PayloadLength);

            switch (frame.Header.OPCODE)
            {
                case WsFrameHeader.Opcodes.CLOSE:
                    await handleCloseMessage(payload, cancellationToken).ConfigureAwait(false);
                    return;

                case WsFrameHeader.Opcodes.PING:
                    await sendPong(payload, cancellationToken).ConfigureAwait(false);
                    break;

                case WsFrameHeader.Opcodes.PONG: // do nothing
                    break;

                default:
                    // TODO: extensions may define additional opcode
                    throw new ProtocolViolationException("Unexpected frame type");
            }
        }

        private async Task handleCloseMessage(ArraySegment<byte> payload, CancellationToken cancellationToken)
        {
            var c = state;
            while (true)
            {
                if (c >= (int)State.CLOSE_RECEIVED) return;
                if (Interlocked.CompareExchange(ref state, (int)State.CLOSE_RECEIVED, c) == c) break;
            }

            try
            {
                if (payload.Count >= 2)
                    await sendCloseFrameAsync(payload, cancellationToken).ConfigureAwait(false);
                else if (payload.Count == 1)
                    await CloseAsync(CloseStatusCode.ProtocolError, cancellationToken).ConfigureAwait(false);
                else
                    await CloseAsync(CloseStatusCode.NormalClosure, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                await cleanClose().ConfigureAwait(false);
            }
        }

        private async Task cleanClose()
        {
            state = (int)State.CLOSED;
            try
            {
                await stream.FlushAsync().ConfigureAwait(false);
            }
            finally
            {
                stream.Close();
            }
        }

        /// <summary> Close websocket (send close frame, then receive until we get a close response message) </summary>
        /// <param name="statusCode">Close status to send</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public Task CloseAsync(CloseStatusCode statusCode, CancellationToken cancellationToken)
        {
            var s = (short)statusCode;
            var array = new[] { (byte)(s >> 8), (byte)(s & 0xff) };
            var payload = new ArraySegment<byte>(array);
            return sendCloseFrameAsync(payload, cancellationToken);
        }

        private async Task sendCloseFrameAsync(ArraySegment<byte> payload, CancellationToken cancellationToken)
        {
            var c = state;
            while (true)
            {
                if (c >= (int)State.SENDING_CLOSE) return;
                if (Interlocked.CompareExchange(ref state, (int)State.SENDING_CLOSE, c) == c) break;
            }

            await SendMessageAsync(WsFrameHeader.Opcodes.CLOSE, payload, cancellationToken).ConfigureAwait(false);

            c = state;
            while (true)
            {
                if (c >= (int)State.CLOSE_SENT) return;
                if (Interlocked.CompareExchange(ref state, (int)State.CLOSE_SENT, c) == c) break;
            }
        }

        private Task sendPong(ArraySegment<byte> payload, CancellationToken cancellationToken)
        {
            return SendMessageAsync(WsFrameHeader.Opcodes.PONG, payload, cancellationToken);
        }

        private static bool isControlFrame(WsFrame frame)
        {
            return frame.Header.OPCODE >= WsFrameHeader.Opcodes.CLOSE;
        }

        /// <summary>
        /// </summary>
        /// <param name="opcode"></param>
        /// <param name="payload"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task SendMessageAsync(WsFrameHeader.Opcodes opcode, ArraySegment<byte> payload, CancellationToken cancellationToken)
        {
            await writeSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // TODO: write data to a "send buffer" first?
                // TODO: cancellationToken may cause invalid data in the channel
                ArraySegment<byte> header = WsDissector.SerializeFrameHeader(new WsFrameHeader { FIN = true, OPCODE = opcode }, payload.Count, null);
                await stream.WriteAsync(header, cancellationToken).ConfigureAwait(false);
                if (payload.Count > 0) await stream.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                writeSemaphore.Release();
            }
        }
    }
}