using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace wslib.Utils
{
    public abstract class StreamWrapper : Stream
    {
        private readonly bool closeInnerStream;
        protected readonly Stream InnerStream;

        public StreamWrapper(Stream innerStream, bool closeInnerStream)
        {
            this.closeInnerStream = closeInnerStream;
            this.InnerStream = innerStream;
        }

        public override void Close()
        {
            if (closeInnerStream) InnerStream.Close();
        }

        public sealed override long Position
        {
            get { return GetPosition(); }
            set { Seek(value, SeekOrigin.Begin); }
        }

        public long GetPosition()
        {
            return InnerStream.Position;
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
            return InnerStream.FlushAsync(cancellationToken);
        }

        public override bool CanRead => InnerStream.CanRead;
        public override bool CanSeek => InnerStream.CanSeek;
        public override bool CanWrite => InnerStream.CanWrite;
        public override long Length => InnerStream.Length;

        public sealed override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            Task<int> t = ReadAsync(buffer, offset, count);
            var result = new TaskAsyncResult<int>(t, state);
            if (callback != null) t.ContinueWith(_ => callback(result));
            return result;
        }

        public sealed override int EndRead(IAsyncResult asyncResult)
        {
            return ((TaskAsyncResult<int>)asyncResult).Result;
        }

        public sealed override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            Task t = WriteAsync(buffer, offset, count);
            var result = new TaskAsyncResult(t, state);
            if (callback != null) t.ContinueWith(_ => callback(result));
            return result;
        }

        public sealed override void EndWrite(IAsyncResult asyncResult)
        {
            ((TaskAsyncResult)asyncResult).Task.Wait();
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return InnerStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return InnerStream.WriteAsync(buffer, offset, count, cancellationToken);
        }
    }
}