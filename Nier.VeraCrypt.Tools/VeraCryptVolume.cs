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
        private static readonly VolumeField s_checksum2Area = new() {Offset = 64, Size = 188};
        private static readonly VolumeField s_volumeHeaderVersion = new() {Offset = 68, Size = 2};
        private static readonly VolumeField s_minProgramVersion = new() {Offset = 70, Size = 2};
        private static readonly VolumeField s_keyChecksum = new() {Offset = 72, Size = 4};
        private static readonly VolumeField s_volumeCreationTime = new() {Offset = 76, Size = 8};
        private static readonly VolumeField s_headerCreationTime = new() {Offset = 84, Size = 8};
        private static readonly VolumeField s_hiddenVolumeSize = new() {Offset = 92, Size = 8};
        private static readonly VolumeField s_volumeSize = new() {Offset = 100, Size = 8};
        private static readonly VolumeField s_masterKeyScopeOffset = new() {Offset = 108, Size = 8};
        private static readonly VolumeField s_masterKeyEncryptionSize = new() {Offset = 116, Size = 8};
        private static readonly VolumeField s_flagBits = new() {Offset = 124, Size = 4};
        private static readonly VolumeField s_sectorSize = new() {Offset = 128, Size = 4};
        private static readonly VolumeField s_checksum2 = new() {Offset = 252, Size = 4};
        private static readonly VolumeField s_keys = new() {Offset = 256, Size = 256};
        private static readonly IBitConverter s_bigEndianBitConverter = BitConverters.BigEndian();
        private const int s_headerSize = 512;

        public string Magic { get; }
        public short VolumeHeaderVersion { get; }

        public short MinProgramVersion { get; }

        public ulong VolumeSize { get; }
        public uint SectorSize { get; }

        public ulong MasterKeyScopeOffset { get; }

        public ulong MasterKeyEncryptionSize { get; }

        private readonly XTS _dataCryptor;
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
            xts.Decrypt(header[64..], decryptedHeaderBytes[64..], 0);
            Magic = ReadAscii(decryptedHeaderBytes, s_magic);
            if (Magic != "VERA")
            {
                var magicHex = BitConverter.ToString(Encoding.ASCII.GetBytes(Magic)).Replace("-", " ");
                throw new Exception($"Invalid magic field '{Magic}' (hex: {magicHex})");
            }

            uint decryptedDataKeysChecksum = ReadUInt(decryptedHeaderBytes, s_keyChecksum);
            var dataKeys = ReadBytes(decryptedHeaderBytes, s_keys);
            uint realDataKeyChecksum = Crc32.Compute(dataKeys);

            if (decryptedDataKeysChecksum != realDataKeyChecksum)
            {
                throw new Exception("mismatch key checksum.");
            }

            var checksum2Bytes = ReadBytes(decryptedHeaderBytes, s_checksum2Area);
            uint decryptedChecksum2 = ReadUInt(decryptedHeaderBytes, s_checksum2);
            uint actualCheckSum2 = Crc32.Compute(checksum2Bytes);
            if (actualCheckSum2 != decryptedChecksum2)
            {
                throw new Exception("mismatch checksum2");
            }

            // all 0 right now, not used
            ulong volumeCreationTime = ReadULong(decryptedHeaderBytes, s_volumeCreationTime);
            ulong headerCreationTime = ReadULong(decryptedHeaderBytes, s_headerCreationTime);
            Console.WriteLine($"volume creation time {volumeCreationTime}, header creation time {headerCreationTime}");

            uint flags = ReadUInt(decryptedHeaderBytes, s_flagBits);
            Console.WriteLine("flags " + flags);

            VolumeHeaderVersion = ReadShort(decryptedHeaderBytes, s_volumeHeaderVersion);
            MinProgramVersion = ReadShort(decryptedHeaderBytes, s_minProgramVersion);
            VolumeSize = ReadULong(decryptedHeaderBytes, s_volumeSize);
            ulong hiddenVolumeSize = ReadULong(decryptedHeaderBytes, s_hiddenVolumeSize);

            SectorSize = ReadUInt(decryptedHeaderBytes, s_sectorSize);
            MasterKeyScopeOffset = ReadULong(decryptedHeaderBytes, s_masterKeyScopeOffset);
            MasterKeyEncryptionSize = ReadULong(decryptedHeaderBytes, s_masterKeyEncryptionSize);

            if (MinProgramVersion < 267)
            {
                throw new Exception("Unsupported volume format v1");
            }

            if (hiddenVolumeSize != 0)
            {
                throw new Exception("does not support hidden volume. hidden volume size " + hiddenVolumeSize);
            }

            var dataKey1 = new AesCipher(dataKeys[..keySize].ToArray());
            var dataKey2 = new AesCipher(dataKeys.Slice(keySize, keySize).ToArray());
            _dataCryptor = new XTS(dataKey1, dataKey2);
        }

        public void ReadDataBytes(Stream output, ulong dataOffset, ulong length, Action<ulong> progressCallback)
        {
            if (dataOffset % SectorSize != 0)
            {
                throw new ArgumentException("offset should align with sector size", nameof(dataOffset));
            }

            if (length % SectorSize != 0)
            {
                throw new ArgumentException("length should align with sector size", nameof(length));
            }

            if (dataOffset > MasterKeyEncryptionSize)
            {
                throw new ArgumentException("offset exceeds limit", nameof(dataOffset));
            }

            if (dataOffset + length > MasterKeyEncryptionSize)
            {
                throw new ArgumentException("length exceeds limit", nameof(length));
            }

            ulong volumeOffset = MasterKeyScopeOffset + dataOffset;
            _fileStream.Seek((long)volumeOffset, SeekOrigin.Begin);
            // read sector by sector
            ulong sectorNum = volumeOffset / SectorSize;
            Span<byte> sectorBuffer = new(new byte[SectorSize]);
            Span<byte> buffer = new(new byte[SectorSize]);
            ulong dataRead = 0;
            progressCallback(dataRead);
            while (dataRead < length)
            {
                ReadAll(_fileStream, sectorBuffer);
                _dataCryptor.Decrypt(sectorBuffer, buffer, sectorNum);
                sectorNum++;
                dataRead += SectorSize;
                progressCallback(dataRead);
                output.Write(buffer);
            }
            output.Flush();
        }

        private void ReadAll(FileStream fileStream, Span<byte> buffer)
        {
            var slice = buffer;
            while (slice.Length > 0)
            {
                int currentRead = fileStream.Read(slice);
                slice = slice[currentRead..];
            }
        }

        private Span<byte> ReadBytes(Span<byte> bytes, VolumeField volumeField)
        {
            return bytes.Slice(volumeField.Offset, volumeField.Size);
        }

        private ulong ReadULong(Span<byte> bytes, VolumeField volumeField)
        {
            Span<byte> fieldBytes = ReadBytes(bytes, volumeField);
            return s_bigEndianBitConverter.ToUInt64(fieldBytes);
        }

        private uint ReadUInt(Span<byte> bytes, VolumeField volumeField)
        {
            Span<byte> fieldBytes = ReadBytes(bytes, volumeField);
            return s_bigEndianBitConverter.ToUInt32(fieldBytes);
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
