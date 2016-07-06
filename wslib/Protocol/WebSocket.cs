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
        private readonly bool serverSide;
        private readonly SemaphoreSlim writeSemaphore = new SemaphoreSlim(1, 1);

        public WebSocket(Dictionary<string, object> env, Stream stream, List<IMessageExtension> extensions, bool serverSide)
        {
            this.env = env;
            this.stream = stream;
            this.extensions = extensions;
            this.serverSide = serverSide;
        }

        public void Dispose()
        {
            writeSemaphore.Dispose();
            stream.Dispose();
        }

        public async Task<WsMessage> ReadMessageAsync(CancellationToken cancellationToken) // TODO: use cancellation token
        {
            try
            {
                while (IsConnected())
                {
                    var frame = await WsDissector.ReadFrameHeader(stream, serverSide).ConfigureAwait(false); // TODO: close connection gracefully
                    if (!isDataFrame(frame))
                    {
                        await processControlFrame(frame).ConfigureAwait(false);
                        continue;
                    }

                    var messageType = frame.Header.OPCODE == WsFrameHeader.Opcodes.TEXT ? MessageType.Text : MessageType.Binary;
                    Stream payloadStream = new WsMesageReadStream(frame, stream, false);
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
                await closeConnection(CloseStatusCode.ProtocolError).ConfigureAwait(false); // TODO: may throw exception?
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
                    // TODO: extensions may define additional opcode
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
                var header = WsDissector.SerializeFrameHeader(new WsFrameHeader(0, 0) { FIN = true, OPCODE = WsFrameHeader.Opcodes.CLOSE }, 2);
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
                var header = WsDissector.SerializeFrameHeader(new WsFrameHeader(0, 0) { FIN = true, OPCODE = WsFrameHeader.Opcodes.PONG }, payload.Length);
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