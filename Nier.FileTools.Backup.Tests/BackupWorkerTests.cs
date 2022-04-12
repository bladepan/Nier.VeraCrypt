using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Nier.FileTools.Backup.Tests
{
    [TestClass]
    public class BackupWorkerTests
    {
        private readonly string dataRootDir = Path.Join("TestData", "BackupWorkerTests");
        private readonly Random random = new();

        [TestInitialize]
        public void Setup()
        {
            Directory.CreateDirectory(dataRootDir);
        }

        [TestCleanup]
        public void Cleanup()
        {
        }

        [TestMethod]
        public async Task RunAsync()
        {
            // data size can be divided by chunk size
            await InvokeRunAsync(128 * 8, 128);
            // data size cannot be divided by chunk size
            await InvokeRunAsync(128 * 8 + 31, 128);
        }


        private async Task InvokeRunAsync(int dataSize, int chunkSize)
        {
            var testId = Guid.NewGuid().ToString("N");
            var inputFileName = Path.Join(dataRootDir, $"input-{testId}");
            var bytes = new byte[dataSize];
            random.NextBytes(bytes);
            await File.WriteAllBytesAsync(inputFileName, bytes);

            var outputDir = Path.Join(dataRootDir, $"output-{testId}");
            Directory.CreateDirectory(outputDir);
            var tempDir = Path.Join(dataRootDir, $"temp-{testId}");
            Directory.CreateDirectory(tempDir);
            var cmdOptions = new CmdLineOptions
            {
                ChunkSize = chunkSize,
                InputFile = new FileInfo(inputFileName),
                OutputDir = new FileInfo(outputDir),
                TempDir = new FileInfo(tempDir)
            };
            var worker = new BackupWorker(cmdOptions, new SysConsoleWrapper());
            var results = await worker.RunAsync();
            var offset = 0;
            foreach (var result in results)
            {
                var resultCode = result.Code;
                if (resultCode == ChunkWriterResultCode.None)
                {
                    continue;
                }

                Assert.AreEqual(ChunkWriterResultCode.MissingChecksumFile, resultCode,
                    $"backup file {result.ChunkIndex} {result.DataFilePath} failed");
                var outputBytes = await File.ReadAllBytesAsync(result.DataFilePath);
                Assert.AreEqual(result.BytesWritten, outputBytes.Length,
                    $"backup file {result.DataFilePath} length is incorrect");
                for (int i = 0; i < result.BytesWritten; i++)
                {
                    Assert.AreEqual(bytes[offset], outputBytes[i], $"byte mismatch at {offset}");
                    offset++;
                }
            }

            Assert.AreEqual(bytes.Length, offset, "invalid output size");
        }
    }
}
