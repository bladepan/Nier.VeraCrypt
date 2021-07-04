using System;

namespace Nier.VeraCrypt.Tools
{
    public static class BitConverters
    {
        public static IBitConverter BigEndian()
        {
            if (BitConverter.IsLittleEndian)
            {
                return new ReverseEndianBitConverter();
            }

            return new SystemEndianBitConverter();
        }
    }
}
