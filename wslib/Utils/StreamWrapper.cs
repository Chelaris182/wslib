using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace wslib.Utils
{
    public abstract class StreamWrapper : Stream
    {
        private readonly bool closeInnerStream;
        private readonly Stream innerStream;

        protected StreamWrapper(Stream innerStream, bool closeInnerStream)
        {
            this.closeInnerStream = closeInnerStream;
            this.innerStream = innerStream;
        }

        protected override void Dispose(bool disposing)
        {
            if (closeInnerStream) innerStream.Dispose();
        }

        public sealed override long Position
        {
            get { return GetPosition(); }
            set { Seek(value, SeekOrigin.Begin); }
        }

        public long GetPosition()
        {
            return innerStream.Position;
        }

        public sealed override void Flush()
        {
            throw new NotImplementedException();
        }

        public sealed override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public sealed override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public sealed override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public sealed override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return innerStream.FlushAsync(cancellationToken);
        }

        public override bool CanRead => innerStream.CanRead;
        public override bool CanSeek => innerStream.CanSeek;
        public override bool CanWrite => innerStream.CanWrite;
        public override long Length => innerStream.Length;

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return innerStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return innerStream.WriteAsync(buffer, offset, count, cancellationToken);
        }
    }
}