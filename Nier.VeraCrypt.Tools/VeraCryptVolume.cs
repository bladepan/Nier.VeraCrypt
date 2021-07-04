using System;
using System.Text;

namespace Nier.VeraCrypt.Tools
{
    public struct VolumeField
    {
        public int Offset { get; init; }
        public int Size { get; init; }
    }

    /// <summary>
    ///  doc/html/VeraCrypt Volume Format Specification.html
    /// </summary>
    public class VeraCryptVolume
    {
        private static readonly VolumeField s_magic = new VolumeField {Offset = 64, Size = 4};
        private static readonly VolumeField s_volumeHeaderVersion = new VolumeField {Offset = 68, Size = 2};
        private static readonly VolumeField s_minProgramVersion = new VolumeField {Offset = 70, Size = 2};
        private static readonly VolumeField s_volumeSize = new VolumeField {Offset = 100, Size = 8};
        private static readonly VolumeField s_masterKeyScopeOffset = new VolumeField {Offset = 108, Size = 8};
        private static readonly VolumeField s_masterKeyEncryptionSize = new VolumeField {Offset = 116, Size = 8};
        private static readonly VolumeField s_sectorSize = new VolumeField {Offset = 128, Size = 4};
        private static readonly VolumeField s_keys = new VolumeField {Offset = 256, Size = 256};
        private static readonly IBitConverter s_bigEndianBitConverter = BitConverters.BigEndian();

        public string ReadMagic(Span<byte> bytes)
        {
            return ReadAscii(bytes, s_magic);
        }

        public short VolumeHeaderVersion(Span<byte> bytes)
        {
            return ReadShort(bytes, s_volumeHeaderVersion);
        }

        public short MinProgramVersion(Span<byte> bytes)
        {
            return ReadShort(bytes, s_minProgramVersion);
        }

        public long ReadVolumeSize(Span<byte> bytes)
        {
            return ReadLong(bytes, s_volumeSize);
        }

        public int ReadSectorSize(Span<byte> bytes)
        {
            return ReadInt(bytes, s_sectorSize);
        }

        public long ReadMasterKeyScopeOffset(Span<byte> bytes)
        {
            return ReadLong(bytes, s_masterKeyScopeOffset);
        }

        public long ReadMasterKeyEncryptionSize(Span<byte> bytes)
        {
            return ReadLong(bytes, s_masterKeyEncryptionSize);
        }

        public Span<byte> ReadKeys(Span<byte> bytes)
        {
            return ReadBytes(bytes, s_keys);
        }

        private Span<byte> ReadBytes(Span<byte> bytes, VolumeField volumeField)
        {
            return bytes.Slice(volumeField.Offset - 64, volumeField.Size);
        }

        private long ReadLong(Span<byte> bytes, VolumeField volumeField)
        {
            Span<byte> fieldBytes = ReadBytes(bytes, volumeField);
            return s_bigEndianBitConverter.ToInt64(fieldBytes);
        }

        private int ReadInt(Span<byte> bytes, VolumeField volumeField)
        {
            Span<byte> fieldBytes = ReadBytes(bytes, volumeField);
            return s_bigEndianBitConverter.ToInt32(fieldBytes);
        }

        private short ReadShort(Span<byte> bytes, VolumeField volumeField)
        {
            Span<byte> fieldBytes = ReadBytes(bytes, volumeField);
            return s_bigEndianBitConverter.ToInt16(fieldBytes);
        }

        private string ReadAscii(Span<byte> bytes, VolumeField volumeField)
        {
            Span<byte> fieldBytes = ReadBytes(bytes, volumeField);
            return Encoding.ASCII.GetString(fieldBytes);
        }
    }
}
