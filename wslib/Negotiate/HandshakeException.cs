using System;

namespace wslib.Negotiate
{
    internal class HandshakeException : Exception
    {
        public HandshakeException(string description) : base(description)
        {
        }
    }
}