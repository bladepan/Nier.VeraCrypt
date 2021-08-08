using System;

namespace Nier.VeraCrypt.Tools
{
    public interface IBitConverter
    {
        short ToInt16(Span<byte> value);
        int ToInt32(Span<byte> value);
        public uint ToUInt32(Span<byte> value);
        long ToInt64(Span<byte> value);
    }
}
