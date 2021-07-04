using System;
using System.IO;
using System.Security.Cryptography;
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
        private static readonly VolumeField s_salt = new() {Offset = 0, Size = 64};
        private static readonly VolumeField s_magic = new() {Offset = 64, Size = 4};
        private static readonly VolumeField s_volumeHeaderVersion = new() {Offset = 68, Size = 2};
        private static readonly VolumeField s_minProgramVersion = new() {Offset = 70, Size = 2};
        private static readonly VolumeField s_volumeSize = new() {Offset = 100, Size = 8};
        private static readonly VolumeField s_masterKeyScopeOffset = new() {Offset = 108, Size = 8};
        private static readonly VolumeField s_masterKeyEncryptionSize = new() {Offset = 116, Size = 8};
        private static readonly VolumeField s_sectorSize = new() {Offset = 128, Size = 4};
        private static readonly VolumeField s_keys = new() {Offset = 256, Size = 256};
        private static readonly IBitConverter s_bigEndianBitConverter = BitConverters.BigEndian();
        private const int s_headerSize = 512;

        public string Magic { get; }
        public short VolumeHeaderVersion { get; }

        public short MinProgramVersion { get; }

        public long VolumeSize { get; }
        public int SectorSize { get; }

        public long MasterKeyScopeOffset { get; }

        public long MasterKeyEncryptionSize { get; }

        private XTS _dataCryptor;
        private readonly FileStream _fileStream;

        public VeraCryptVolume(string filePath, string password)
        {
            _fileStream = File.OpenRead(filePath);
            if (_fileStream.Length <= s_headerSize)
            {
                throw new Exception("Invalid volume, size is less than header.");
            }
            var header = new Span<byte>(new byte[s_headerSize]);
            ReadAll(_fileStream, header);

            var salt = ReadBytes(header, s_salt);

            var rfc2898DeriveBytes =
                new Rfc2898DeriveBytes(password, salt.ToArray(), 500000, HashAlgorithmName.SHA512);
            byte[] keyBytes = rfc2898DeriveBytes.GetBytes(64);

            int keySize = keyBytes.Length / 2;
            var key1 = new AesCipher(keyBytes[..keySize]);
            var key2 = new AesCipher(keyBytes[keySize..]);
            var xts = new XTS(key1, key2);
            var decryptedHeaderBytes = new Span<byte>(new byte[s_headerSize]);
            xts.Decrypt(header.Slice(64), decryptedHeaderBytes.Slice(64), 0);
            Magic = ReadAscii(decryptedHeaderBytes, s_magic);
            if (Magic != "VERA")
            {
                throw new Exception("Invalid magic field " + Magic);
            }

            VolumeHeaderVersion = ReadShort(decryptedHeaderBytes, s_volumeHeaderVersion);
            MinProgramVersion = ReadShort(decryptedHeaderBytes, s_minProgramVersion);
            VolumeSize = ReadLong(decryptedHeaderBytes, s_volumeSize);
            SectorSize = ReadInt(decryptedHeaderBytes, s_sectorSize);
            MasterKeyScopeOffset = ReadLong(decryptedHeaderBytes, s_masterKeyScopeOffset);
            MasterKeyEncryptionSize = ReadLong(decryptedHeaderBytes, s_masterKeyEncryptionSize);

            var dataKeys = ReadBytes(decryptedHeaderBytes, s_keys);
            var dataKey1 = new AesCipher(dataKeys.Slice(0, keySize).ToArray());
            var dataKey2 = new AesCipher(dataKeys.Slice(keySize, keySize).ToArray());
            _dataCryptor = new XTS(dataKey1, dataKey2);
        }

        public void ReadAllDataBytes(Span<byte> buffer)
        {
            _fileStream.Seek(MasterKeyScopeOffset, SeekOrigin.Begin);
            // read sector by sector
            ulong sectorNum = 0;
            int offset = 0;
            Span<byte> sectorBuffer = new Span<byte>(new byte[SectorSize]);
            while (offset < buffer.Length)
            {
                ReadAll(_fileStream, sectorBuffer);
                _dataCryptor.Decrypt(sectorBuffer, buffer.Slice(offset, SectorSize), sectorNum);
                sectorNum++;
                offset += SectorSize;
            }
        }

        private void ReadAll(FileStream fileStream, Span<byte> buffer)
        {
            int dataRead = 0;
            var slice = buffer;
            while (dataRead < buffer.Length)
            {
                int currentRead = fileStream.Read(slice);
                dataRead += currentRead;
                slice = slice.Slice(currentRead);
            }
        }

        private Span<byte> ReadBytes(Span<byte> bytes, VolumeField volumeField)
        {
            return bytes.Slice(volumeField.Offset, volumeField.Size);
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
