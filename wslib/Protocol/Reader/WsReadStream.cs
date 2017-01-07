using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
#if NETFX
using wslib.Utils;
#endif

namespace wslib.Protocol.Reader
{
    class WsReadStream : Stream
    {
        private readonly WsMesageReader wsMesageReader;

        public WsReadStream(WsMesageReader wsMesageReader)
        {
            this.wsMesageReader = wsMesageReader;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return wsMesageReader.ReadAsync(buffer, offset, count, cancellationToken);
        }

#if NETFX
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
#endif // NETFX

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { throw new NotImplementedException(); }
        }

        public override long Position
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
    }
}