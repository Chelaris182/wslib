using System;
using System.Net;

namespace wslib.Protocol
{
    public class WsFrameHeader
    {
        public bool FIN
        {
            get { return byte1.HasFlag(7); }
            set { ByteExtensions.SetFlag(ref byte1, 7, value); }
        }

        public bool RSV1
        {
            get { return byte1.HasFlag(6); }
            set { ByteExtensions.SetFlag(ref byte1, 6, value); }
        }

        public bool RSV2
        {
            get { return byte1.HasFlag(5); }
            set { ByteExtensions.SetFlag(ref byte1, 5, value); }
        }

        public bool RSV3
        {
            get { return byte1.HasFlag(4); }
            set { ByteExtensions.SetFlag(ref byte1, 4, value); }
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
            set { byte1 = (byte)((byte1 & 0xF0) | (byte)value); }
        }

        public bool MASK => byte2.HasFlag(7);

        private byte byte1;
        private readonly byte byte2;

        public void CopyTo(ArraySegment<byte> arraySegment)
        {
            if (arraySegment.Count < 2) throw new ArgumentException("not enough data to copy frame header");
            arraySegment.Array[arraySegment.Offset] = byte1;
            arraySegment.Array[arraySegment.Offset + 1] = byte2;
        }

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

        public static void SetFlag(ref byte b, int num, bool value)
        {
            if (value)
                b |= (byte)(1 << num);
            else
                b &= (byte)~(1 << num);
        }
    }
}