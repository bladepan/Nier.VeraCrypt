using System;
using System.Linq;

namespace Nier.VeraCrypt.Tools
{
    public class ReverseEndianBitConverter : IBitConverter
    {
        public short ToInt16(Span<byte> value)
        {
            byte[] reversed = value.ToArray().Reverse().ToArray();
            return BitConverter.ToInt16(reversed);
        }

        public long ToInt64(Span<byte> value)
        {
            byte[] reversed = value.ToArray().Reverse().ToArray();
            return BitConverter.ToInt64(reversed);
        }
    }
}
