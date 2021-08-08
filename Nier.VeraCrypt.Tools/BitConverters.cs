using System;

namespace Nier.VeraCrypt.Tools
{
    public static class BitConverters
    {
        public static IBitConverter LittleEndian()
        {
            if (BitConverter.IsLittleEndian)
            {
                return new SystemEndianBitConverter();
            }

            return new ReverseEndianBitConverter();
        }

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
