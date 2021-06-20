using System;
using System.Security.Cryptography;

namespace Nier.VeraCrypt.Tools
{
    public interface IBlockCipher
    {
        int BlockSize { get; }
        void Encrypt(Span<byte> dst, Span<byte> src);
        void Decrypt(Span<byte> dst, Span<byte> src);
    }

    public class AesCipher : IBlockCipher
    {
        private readonly byte[] _key;
        private readonly Aes _aes;

        public AesCipher(byte[] key)
        {
            _key = key;
            _aes = Aes.Create();
            _aes.Key = _key;

            BlockSize = _aes.BlockSize / 8;
            // 0 iv
            _aes.IV = new byte[BlockSize];
        }

        public int BlockSize { get; }

        public void Encrypt(Span<byte> dst, Span<byte> src)
        {
            var encryptor = _aes.CreateEncryptor();
            // todo extract
            byte[] inputBytes = src.ToArray();
            byte[] outputBytes = new byte[inputBytes.Length];
            var inputOffset = 0;
            var inputCount = BlockSize;
            while (inputCount > 0)
            {
                var transformed =
                    encryptor.TransformBlock(inputBytes, inputOffset, inputCount, outputBytes, inputOffset);
                inputOffset += transformed;
                inputCount -= transformed;
            }

            outputBytes.AsSpan().CopyTo(dst);
        }

        public void Decrypt(Span<byte> dst, Span<byte> src)
        {
            var decryptor = _aes.CreateDecryptor();
            byte[] inputBytes = src.ToArray();
            byte[] outputBytes = new byte[BlockSize];
            var inputOffset = 0;
            var inputCount = BlockSize;
            while (inputCount > 0)
            {
                var transformedBytes = decryptor.TransformBlock(inputBytes, inputOffset, inputCount,
                    outputBytes, inputOffset);
                inputOffset += transformedBytes;
                inputCount -= transformedBytes;
            }

            outputBytes.AsSpan().CopyTo(dst);
        }
    }

    /// <summary>
    /// ported from https://github.com/golang/crypto/blob/master/xts/xts.go
    /// </summary>
    public class XTS
    {
        private readonly IBlockCipher _k1;
        private readonly IBlockCipher _k2;
        private readonly int _blockSize;

        public XTS(IBlockCipher k1, IBlockCipher k2)
        {
            _k1 = k1;
            _k2 = k2;
            _blockSize = _k1.BlockSize;
        }

        public void Encrypt(Span<byte> ciphertext, Span<byte> plaintext, ulong sectorNum)
        {
            if (plaintext.Length % _blockSize != 0)
            {
                throw new Exception("invalid plaintext size");
            }

            byte[] tweak = new byte[_blockSize];
            if (!BitConverter.TryWriteBytes(tweak, sectorNum))
            {
                throw new Exception("failed to write sector num to tweak");
            }

            _k2.Encrypt(tweak, tweak);

            while (plaintext.Length > 0)
            {
                for (int i = 0; i < _blockSize; i++)
                {
                    ciphertext[i] = (byte) (plaintext[i] ^ tweak[i]);
                }

                _k1.Encrypt(ciphertext, ciphertext);
                for (int i = 0; i < _blockSize; i++)
                {
                    ciphertext[i] ^= tweak[i];
                }

                plaintext = plaintext[_blockSize..];
                ciphertext = ciphertext[_blockSize..];
                Mul2(tweak);
            }
        }

        public void Decrypt(Span<byte> ciphertext, Span<byte> plaintext, ulong sectorNum)
        {
            if (ciphertext.Length % _blockSize != 0)
            {
                throw new Exception("invalid cipher text length");
            }

            byte[] tweak = new byte[_blockSize];
            if (!BitConverter.TryWriteBytes(tweak, sectorNum))
            {
                throw new Exception("failed to write sector num to tweak");
            }

            _k2.Encrypt(tweak, tweak);

            while (ciphertext.Length > 0)
            {
                for (int i = 0; i < _blockSize; i++)
                {
                    plaintext[i] = (byte) (ciphertext[i] ^ tweak[i]);
                }

                _k1.Decrypt(plaintext, plaintext);
                for (int i = 0; i < _blockSize; i++)
                {
                    plaintext[i] ^= tweak[i];
                }

                plaintext = plaintext[_blockSize..];
                ciphertext = ciphertext[_blockSize..];
                Mul2(tweak);
            }
        }

        private static void Mul2(byte[] tweak)
        {
            byte carryIn = 0;
            for (int i = 0; i < tweak.Length; i++)
            {
                byte carryOut = (byte) (tweak[i] >> 7);
                tweak[i] = (byte) ((tweak[i] << 1) + carryIn);
                carryIn = carryOut;
            }

            if (carryIn != 0)
            {
                tweak[0] ^= 1 << 7 | 1 << 2 | 1 << 1 | 1;
            }
        }
    }
}