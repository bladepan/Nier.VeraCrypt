using System;

namespace Nier.VeraCrypt.Tools
{
    public interface IBitConverter
    {
        short ToInt16(Span<byte> value);
        long ToInt64(Span<byte> value);
    }
}
