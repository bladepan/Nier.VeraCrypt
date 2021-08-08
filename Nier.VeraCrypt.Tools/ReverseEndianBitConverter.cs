using System;
using System.Linq;

namespace Nier.VeraCrypt.Tools
{
    public class ReverseEndianBitConverter : IBitConverter
    {
        public short ToInt16(Span<byte> value)
        {
            byte[] reversed = GetReversedCopy(value);
            return BitConverter.ToInt16(reversed);
        }

        public int ToInt32(Span<byte> value)
        {
            byte[] reversed = GetReversedCopy(value);
            return BitConverter.ToInt32(reversed);
        }

        public uint ToUInt32(Span<byte> value)
        {
            byte[] reversed = GetReversedCopy(value);
            return BitConverter.ToUInt32(reversed);
        }

        public long ToInt64(Span<byte> value)
        {
            byte[] reversed = GetReversedCopy(value);
            return BitConverter.ToInt64(reversed);
        }

        private static byte[] GetReversedCopy(Span<byte> value) => value.ToArray().Reverse().ToArray();
    }
}
