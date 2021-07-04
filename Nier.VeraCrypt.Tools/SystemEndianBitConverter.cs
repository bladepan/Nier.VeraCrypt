using System;

namespace Nier.VeraCrypt.Tools
{
    public class SystemEndianBitConverter : IBitConverter
    {
        public short ToInt16(Span<byte> value) => BitConverter.ToInt16(value);
        public int ToInt32(Span<byte> value) => BitConverter.ToInt32(value);

        public long ToInt64(Span<byte> value) => BitConverter.ToInt64(value);
    }
}
