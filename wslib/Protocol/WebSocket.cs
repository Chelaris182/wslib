using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace wslib.Protocol
{
    public class WebSocket : IWebSocket
    {
        public bool IsConnected() => stream.CanRead;

        private Dictionary<string, object> env;
        private readonly Stream stream;
        private readonly bool server;
        private readonly SemaphoreSlim writeSemaphore = new SemaphoreSlim(1, 1);

        public WebSocket(Dictionary<string, object> env, Stream stream, bool server = true)
        {
            this.env = env;
            this.stream = stream;
            this.server = server;
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
                    var frame = await WsDissector.ReadFrameHeader(stream, server).ConfigureAwait(false); // TODO: close connection gracefully
                    if (!isDataFrame(frame))
                    {
                        await processControlFrame(frame).ConfigureAwait(false);
                        continue;
                    }

                    var messageType = frame.Header.OPCODE == WsFrameHeader.Opcodes.TEXT ? MessageType.Text : MessageType.Binary;
                    var messagePayload = new WsReadStream(frame, stream, false);
                    return new WsMessage(messageType, messagePayload);
                }
            }
            catch (IOException e) // happens when read or write returns error
            {
                stream.Close();
            }
            catch (ProtocolViolationException e)
            {
                await closeConnection(CloseStatusCode.ProtocolError).ConfigureAwait(false); // TODO: may throw exception?
            }
            catch (InvalidOperationException e) // happens when read or write happens on a closed socket
            {
                // TODO: log?
            }

            return null;
        }

        public async Task<WsMessageWriter> CreateWriter(MessageType type, CancellationToken cancellationToken)
        {
            await writeSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            return new WsMessageWriter(type, () => writeSemaphore.Release(), stream); // TODO replace action with disposable object
        }

        private async Task processControlFrame(WsFrame frame)
        {
            if (frame.PayloadLength > 1024)
            {
                await closeConnection(CloseStatusCode.MessageTooLarge).ConfigureAwait(false);
                return;
            }

            byte[] payload = new byte[frame.PayloadLength];
            await stream.ReadUntil(payload, 0, payload.Length).ConfigureAwait(false);

            switch (frame.Header.OPCODE)
            {
                case WsFrameHeader.Opcodes.CLOSE:
                    if (payload.Length >= 2)
                        await closeConnection(payload[0], payload[1]).ConfigureAwait(false);
                    else
                        await closeConnection(CloseStatusCode.NormalClosure).ConfigureAwait(false);
                    return;

                case WsFrameHeader.Opcodes.PING:
                    pongReply(frame, payload); // fire&forget
                    break;

                default:
                    throw new ProtocolViolationException("Unexpected frame type");
            }
        }

        private Task closeConnection(CloseStatusCode statusCode)
        {
            var s = (short)statusCode;
            return closeConnection((byte)(s >> 8), (byte)(s & 0xff));
        }

        private async Task closeConnection(byte code1, byte code2)
        {
            await writeSemaphore.WaitAsync(CancellationToken.None).ConfigureAwait(false); // TODO: cancellation token / timeout
            try
            {
                var header = WsDissector.CreateFrameHeader(true, WsFrameHeader.Opcodes.CLOSE, 2);
                await stream.WriteAsync(header.Array, header.Offset, header.Count).ConfigureAwait(false);
                await stream.WriteAsync(new[] { code1, code2 }, 0, 2).ConfigureAwait(false);
                stream.Close();
            }
            finally
            {
                writeSemaphore.Release(); // TODO: replace with disposable object
            }
        }

        private async Task pongReply(WsFrame frame, byte[] payload)
        {
            await writeSemaphore.WaitAsync(CancellationToken.None).ConfigureAwait(false); // TODO: cancellation token / timeout
            try
            {
                var header = WsDissector.CreateFrameHeader(true, WsFrameHeader.Opcodes.PONG, 2);
                await stream.WriteAsync(header.Array, header.Offset, header.Count).ConfigureAwait(false);
                await stream.WriteAsync(payload, 0, payload.Length).ConfigureAwait(false);
            }
            finally
            {
                writeSemaphore.Release(); // TODO: replace with disposable object
            }
        }

        private static bool isDataFrame(WsFrame frame)
        {
            return frame.Header.OPCODE == WsFrameHeader.Opcodes.BINARY || frame.Header.OPCODE == WsFrameHeader.Opcodes.TEXT;
        }
    }
}