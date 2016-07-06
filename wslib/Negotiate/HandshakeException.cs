using System;

namespace wslib.Negotiate
{
    public class HandshakeException : Exception
    {
        public HandshakeException(string description) : base(description)
        {
        }
    }
}