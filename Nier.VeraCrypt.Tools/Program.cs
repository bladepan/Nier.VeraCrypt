using System;
using System.IO;
using System.Text;

namespace Nier.VeraCrypt.Tools
{
    class Program
    {
        public void Read()
        {
            var password = "test1";
            var filePath = "/home/pan/Documents/test.disk";

            VeraCryptVolume v = new(filePath, password);
            Console.WriteLine("magic "+ v.Magic);
            Console.WriteLine("header version " + v.VolumeHeaderVersion);
            Console.WriteLine("min program Version " + v.MinProgramVersion);
            Console.WriteLine("volume size " + v.VolumeSize);
            Console.WriteLine("master key scope " + v.MasterKeyScopeOffset);
            Console.WriteLine("sector size " + v.SectorSize);
            Console.WriteLine("master key encryption size " + v.MasterKeyEncryptionSize);
            var databytes = new Span<byte>(new byte[v.MasterKeyEncryptionSize]);
            v.ReadAllDataBytes(databytes);
            // File.WriteAllBytes("/home/pan/Documents/dump.bin", databytes.ToArray());
            var offset = 0;
            int batchSize = 1024;
            while (offset < databytes.Length)
            {
                Span<byte> dataSlice;
                if (offset + batchSize > databytes.Length)
                {
                    dataSlice = databytes.Slice(offset);
                }
                else
                {
                    dataSlice = databytes.Slice(offset, batchSize);
                }

                string str = Encoding.UTF8.GetString(dataSlice);
                if (str.Contains("Hello"))
                {
                    Console.WriteLine(offset);
                    Console.WriteLine(str);
                }

                offset += batchSize;
            }
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
            var p = new Program();
            p.Read();
        }
    }
}
