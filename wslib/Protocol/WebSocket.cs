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
        private readonly SemaphoreSlim writeSemaphore = new SemaphoreSlim(1, 1);

        public WebSocket(Dictionary<string, object> env, Stream stream)
        {
            this.env = env;
            this.stream = stream;
        }

        public void Dispose()
        {
            writeSemaphore.Dispose();
            stream.Dispose();
        }

        public async Task<WsMessage> ReadMessageAsync(CancellationToken cancellationToken)
        {
            while (IsConnected())
            {
                var frame = await WsDissector.ReadFrameHeader(stream).ConfigureAwait(false); // TODO: close connection gracefully
                if (!isDataFrame(frame))
                {
                    await processControlFrame(frame).ConfigureAwait(false);
                    continue;
                }

                var messageType = frame.Header.OPCODE == WsFrameHeader.Opcodes.TEXT ? MessageType.Text : MessageType.Binary;
                var messagePayload = new WsReadStream(frame, stream, false);
                return new WsMessage(messageType, messagePayload);
            }

            return null;
        }

        public async Task<WsMessageWriter> CreateWriter(MessageType type, CancellationToken cancellationToken)
        {
            await writeSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            return new WsMessageWriter(type, () => writeSemaphore.Release(), stream); // TODO replase action with disposable object
        }

        private async Task processControlFrame(WsFrame frame)
        {
            if (frame.PayloadLength > 1024)
            {
                closeConnection(CloseStatusCode.MessageTooLarge);
                return;
            }

            byte[] payload = new byte[frame.PayloadLength];
            await stream.ReadUntil(payload, 0, payload.Length).ConfigureAwait(false);

            switch (frame.Header.OPCODE)
            {
                case WsFrameHeader.Opcodes.CLOSE:
                    int code = (int)StreamExtensions.ReadN(payload, 0, 2); // client clode status code
                    closeConnection((CloseStatusCode)code);
                    return;

                case WsFrameHeader.Opcodes.PING:
                    pongReply(frame); // fire&forget
                    break;

                default:
                    throw new ProtocolViolationException("Unexpected frame type");
            }
        }

        private void closeConnection(CloseStatusCode closeCode)
        {
        }

        private async Task pongReply(WsFrame frame)
        {
        }

        private static bool isDataFrame(WsFrame frame)
        {
            return frame.Header.OPCODE == WsFrameHeader.Opcodes.BINARY || frame.Header.OPCODE == WsFrameHeader.Opcodes.TEXT;
        }
    }
}