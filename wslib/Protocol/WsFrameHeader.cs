using System;
using System.Net;

namespace wslib.Protocol
{
    public class WsFrameHeader
    {
        public bool FIN
        {
            get { return byte1.HasFlag(7); }
        }

        public bool RSV1
        {
            get { return byte1.HasFlag(6); }
        }

        public enum Opcodes
        {
            CONTINUATION = 0,
            TEXT = 1,
            BINARY = 2,
            CLOSE = 8,
            PING = 9,
            PONG = 0x0A
        }

        public Opcodes OPCODE
        {
            get { return (Opcodes)(byte1 & 0x0F); }
        }

        public bool MASK
        {
            get { return byte2.HasFlag(7); }
        }

        private readonly byte byte1;
        private readonly byte byte2;

        public WsFrameHeader(byte byte1, byte byte2)
        {
            this.byte1 = byte1;
            this.byte2 = byte2;

            if (!Enum.IsDefined(typeof(Opcodes), OPCODE))
                throw new ProtocolViolationException("Unknown opcode");
        }
    }

    static class ByteExtensions
    {
        public static bool HasFlag(this byte b, int num)
        {
            return (b & (1 << num)) != 0;
        }
    }
}