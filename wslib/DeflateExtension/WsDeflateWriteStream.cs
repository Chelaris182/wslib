using System;
using System.IO.Compression;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using wslib.Protocol;
using wslib.Protocol.Writer;

namespace wslib.DeflateExtension
{
    class WsDeflateWriteStream : IWsMessageWriteStream
    {
        private readonly IWsMessageWriteStream innerStream;
        private readonly DeflateStream deflateStream;

        public static WsDeflateWriteStream Create(IWsMessageWriteStream innerStream)
        {
            var deflateStream = new DeflateStream(innerStream, CompressionMode.Compress);
            return new WsDeflateWriteStream(innerStream, deflateStream);
        }

        private WsDeflateWriteStream(IWsMessageWriteStream innerStream, DeflateStream deflateStream) : base(deflateStream, false)
        {
            this.innerStream = innerStream;
            this.deflateStream = deflateStream;
        }

        public override Task WriteHeader(WsFrameHeader.Opcodes opcode, bool finFlag, bool rsv1, int payloadLen, CancellationToken cancellationToken)
        {
            return innerStream.WriteHeader(opcode, finFlag, true, payloadLen, cancellationToken);
        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            if (true /* this.wroteBytes*/)
            {
                deflateStream.WriteDeflaterOutput(true); // TODO: sync code inside
                bool flag;
                do
                {
                    int bytesRead;
                    flag = deflateStream.Finish(deflateStream.Buffer(), out bytesRead);
                    if (bytesRead > 0)
                        await innerStream.WriteAsync(deflateStream.Buffer(), 0, bytesRead, cancellationToken).ConfigureAwait(false);
                } while (!flag);
            }

            await innerStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public static class DeflateStreamExtensions
    {
        private static readonly MethodInfo writeDeflaterOutput;
        private static readonly MethodInfo doWrite;
        private static readonly FieldInfo bufferField;
        private static readonly FieldInfo formatWriter;
        private static MethodInfo formatWriterGetFooter;
        private static readonly FieldInfo deflaterField;
        private static MethodInfo deflatorFinish;

        static DeflateStreamExtensions()
        {
            var type = typeof(DeflateStream);
            writeDeflaterOutput = type.GetMethod("WriteDeflaterOutput", BindingFlags.NonPublic | BindingFlags.Instance);
            doWrite = type.GetMethod("DoWrite", BindingFlags.NonPublic | BindingFlags.Instance);
            bufferField = type.GetField("buffer", BindingFlags.NonPublic | BindingFlags.Instance);
            deflaterField = type.GetField("deflater", BindingFlags.NonPublic | BindingFlags.Instance);
            formatWriter = type.GetField("formatWriter", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public static void WriteDeflaterOutput(this DeflateStream deflateStream, bool isAsync)
        {
            writeDeflaterOutput.Invoke(deflateStream, new object[] { isAsync });
        }

        public static void DoWrite(this DeflateStream deflateStream, byte[] array, int offset, int count, bool isAsync)
        {
            doWrite.Invoke(deflateStream, new object[] { array, offset, count, isAsync });
        }

        public static bool Finish(this DeflateStream deflateStream, byte[] array, out int byteRead)
        {
            object deflator = deflaterField.GetValue(deflateStream); // IDeflater
            if (deflatorFinish == null)
            {
                Type typeFoo = deflator.GetType();
                deflatorFinish = typeFoo.GetMethod("System.IO.Compression.IDeflater.Finish", BindingFlags.NonPublic |
                                                                                             BindingFlags.Public |
                                                                                             BindingFlags.Static |
                                                                                             BindingFlags.Instance |
                                                                                             BindingFlags.DeclaredOnly);
            }

            byteRead = 0;
            var args = new object[] { array, byteRead };
            var result = deflatorFinish.Invoke(deflator, args);
            byteRead = (int)args[1];
            return (bool)result;
        }

        public static byte[] Buffer(this DeflateStream deflateStream)
        {
            return bufferField.GetValue(deflateStream) as byte[];
        }

        public static byte[] GetFooter(this DeflateStream deflateStream)
        {
            object formatter = formatWriter.GetValue(deflateStream); // IFileFormatWriter
            if (formatter == null) return null;
            if (formatWriterGetFooter == null)
            {
                Type typeFoo = formatter.GetType();
                formatWriterGetFooter = typeFoo.GetMethod("GetFooter", BindingFlags.Instance);
            }

            var result = formatWriterGetFooter.Invoke(formatter, null);
            return result as byte[];
        }
    }
}