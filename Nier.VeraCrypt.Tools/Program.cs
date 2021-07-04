using System;
using System.IO;
using System.Security.Cryptography;

namespace Nier.VeraCrypt.Tools
{
    class Program
    {
        public void Read()
        {
            var password = "test1";
            var filePath = "/home/pan/Documents/test.disk";
            var fileBytes = File.ReadAllBytes(filePath);
            var salt = new byte[64];
            Array.Copy(fileBytes, salt, salt.Length);
            var rfc2898DeriveBytes =
                new Rfc2898DeriveBytes(password, salt, 500000, HashAlgorithmName.SHA512);
            byte[] keyBytes = rfc2898DeriveBytes.GetBytes(64);
            Console.WriteLine(Convert.ToHexString(keyBytes));
            var keySize = keyBytes.Length / 2;
            var key1 = new AesCipher(keyBytes[..keySize]);
            var key2 = new AesCipher(keyBytes[keySize..]);
            var xts = new XTS(key1, key2);
            var cipherBytes = fileBytes[64..131072];
            var plainText = new byte[cipherBytes.Length];
            xts.Decrypt(cipherBytes, plainText, 0);

            VeraCryptVolume v = new();
            Console.WriteLine("magic "+ v.ReadMagic(plainText));
            Console.WriteLine("header version " + v.ReadHeaderVersion(plainText));
            Console.WriteLine("Version " + v.ReadVersion(plainText));
            Console.WriteLine("volume size " + v.ReadVolumeSize(plainText));

        }

        // encryption algorithm aes
        // hash algorithm sha-512
        // volume size 64MB
        // password test1
        // filesystem ext4

        // aes, block size 16, key size 32
        // xts mode The size of each data unit is always 512 bytes (regardless of the sector size).
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var p = new Program();
            p.Read();
        }
    }
}
