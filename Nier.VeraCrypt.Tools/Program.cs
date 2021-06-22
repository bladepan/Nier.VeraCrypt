using System;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Unicode;

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
            Rfc2898DeriveBytes rfc2898DeriveBytes =
                new Rfc2898DeriveBytes(password, salt, 500000, HashAlgorithmName.SHA512);
            byte[] keyBytes = rfc2898DeriveBytes.GetBytes(64);
            Console.WriteLine(Convert.ToHexString(keyBytes));
            var keySize = keyBytes.Length / 2;
            var key1 = new AesCipher(keyBytes[..keySize]);
            var key2 = new AesCipher(keyBytes[keySize..]);
            var xts = new XTS(key1, key2);
            var cipherBytes = fileBytes[64..512];
            var plainText = new byte[cipherBytes.Length];
            xts.Decrypt(cipherBytes, plainText, 0);
            Console.WriteLine(Convert.ToHexString(plainText)); 
            
            Console.WriteLine(Encoding.ASCII.GetString(plainText));
            // VERA 56455241 Console.WriteLine(Convert.ToHexString(Encoding.ASCII.GetBytes("VERA")));
            Console.WriteLine(Encoding.ASCII.GetString(plainText).Contains("VERA"));
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