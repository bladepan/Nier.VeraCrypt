using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Nier.FileTools.Backup
{
    public class ChunkWriterOptions
    {
        public FileStream InputFileStream { get; init; }
        public int ChunkIndex { get; init; }
        public int ChunkSize { get; init; }
        public FileInfo OutputDir { get; init; }
        public FileInfo TempDir { get; init; }
        public IConsoleWrapper Console { get; init; }
    }

    public enum ChunkWriterResultCode
    {
        None,
        Skip,
        MissingChecksumFile,
        MissingDataFile,
        DataFileSizeMismatch,
        ChecksumMismatch
    }

    public class ChunkWriterResult
    {
        public ChunkWriterResultCode Code { get; init; }

        public int ChunkIndex { get; init; }

        public string DataFilePath { get; init; }
        public int BytesWritten { get; init; }
        public string CheckSum { get; init; }
    }

    public class ChunkWriter
    {
        private readonly FileStream _inputFileStream;
        private readonly int _chunkIndex;
        private readonly int _chunkSize;
        private readonly FileInfo _outputDir;
        private readonly FileInfo _tempDir;
        private readonly IConsoleWrapper _console;

        private readonly string _dataFilePath;
        private readonly string _dataCheckSumPath;
        private readonly string _tempFilePath;
        private readonly long _inputOffset;
        private readonly int _byteCount;

        public ChunkWriter(ChunkWriterOptions options)
        {
            _inputFileStream = options.InputFileStream;
            _chunkIndex = options.ChunkIndex;
            _chunkSize = options.ChunkSize;
            _outputDir = options.OutputDir;
            _tempDir = options.TempDir;
            _console = options.Console;

            _dataFilePath = Path.Join(_outputDir.FullName, $"{_chunkIndex}.data");
            _dataCheckSumPath = Path.Join(_outputDir.FullName, $"{_chunkIndex}.sha256");
            _tempFilePath = Path.Join(_tempDir.FullName, $"{Guid.NewGuid():N}-{_chunkIndex}.data");
            _inputOffset = _chunkIndex * _chunkSize;

            _byteCount = _chunkSize;
            if (_inputOffset >= _inputFileStream.Length)
            {
                _byteCount = 0;
            }
            else
            {
                if (_inputOffset + _byteCount > _inputFileStream.Length)
                {
                    _byteCount = (int)(_inputFileStream.Length - _inputOffset);
                }
            }
        }

        public async Task<ChunkWriterResult> WriteChunk()
        {
            // fake chunk, return
            if (_byteCount == 0)
            {
                return Result(ChunkWriterResultCode.None, null);
            }

            if (!File.Exists(_dataCheckSumPath))
            {
                _console.Info($"{_dataCheckSumPath} does not exist. Writing entire chunk.");
                string checksum = await OverwriteAllAsync();
                return Result(ChunkWriterResultCode.MissingChecksumFile, checksum);
            }

            if (!File.Exists(_dataFilePath))
            {
                _console.Info($"{_dataFilePath} does not exist. Writing entire chunk.");
                string checksum = await OverwriteAllAsync();
                return Result(ChunkWriterResultCode.MissingDataFile, checksum);
            }

            var dataFileStream = File.Open(_dataFilePath, FileMode.OpenOrCreate);
            // not true for last chunk
            if (dataFileStream.Length != _byteCount)
            {
                _console.Info(
                    $"{_dataFilePath} length {dataFileStream.Length} does not match chunk size {_byteCount}. Writing entire chunk.");
                dataFileStream.Close();
                string checksum = await OverwriteAllAsync();
                return Result(ChunkWriterResultCode.DataFileSizeMismatch, checksum);
            }

            string dataCheckSumStr = await File.ReadAllTextAsync(_dataCheckSumPath, Encoding.UTF8);
            byte[] checkSum = await ComputeHashAsync();
            string checkSumStr = Convert.ToBase64String(checkSum);
            if (dataCheckSumStr != checkSumStr)
            {
                _console.Info(
                    $"{_dataFilePath} checksum {dataCheckSumStr} does not match source data checksum {checkSumStr}. Writing entire chunk");
                await WriteTempFileAsync();
                OverwriteDataFile();
                await WriteHashFileAsync(checkSum);
                return Result(ChunkWriterResultCode.ChecksumMismatch, checkSumStr);
            }

            _console.Info(
                $"{_dataFilePath} checksum {dataCheckSumStr} matches source data checksum. Ignore writing chunk.");
            return Result(ChunkWriterResultCode.Skip, checkSumStr);
        }

        private ChunkWriterResult Result(ChunkWriterResultCode code, string checkSum)
        {
            int bytesWritten = _byteCount;
            if (code == ChunkWriterResultCode.Skip)
            {
                bytesWritten = 0;
            }

            return new ChunkWriterResult
            {
                Code = code,
                ChunkIndex = _chunkIndex,
                DataFilePath = _dataFilePath,
                BytesWritten = bytesWritten,
                CheckSum = checkSum
            };
        }

        private async Task<byte[]> ComputeHashAsync()
        {
            await using var sha = new ShaFileStreamBufferConsumer();
            await FileStreamUtils.ReadAsync(_inputFileStream, _inputOffset, _byteCount, sha);
            return sha.GetHash();
        }

        private Task WriteHashFileAsync(byte[] hash)
        {
            string hashStr = Convert.ToBase64String(hash);
            _console.Info($"writing {hashStr} to {_dataCheckSumPath}");
            return File.WriteAllTextAsync(_dataCheckSumPath, hashStr, Encoding.UTF8);
        }

        private async Task<string> OverwriteAllAsync()
        {
            byte[] checksum = await WriteTempFileWithCheckSumAsync();
            OverwriteDataFile();
            await WriteHashFileAsync(checksum);
            return Convert.ToBase64String(checksum);
        }

        private void OverwriteDataFile()
        {
            _console.Info($"mv {_tempFilePath} {_dataFilePath}");
            File.Move(_tempFilePath, _dataFilePath, true);
        }

        private async Task<byte[]> WriteTempFileWithCheckSumAsync()
        {
            _console.Info($"writing data to {_tempFilePath}");
            await using var tempFileWriter = new WriteFileFileStreamBufferConsumer(_tempFilePath);
            await using var shaComputer = new ShaFileStreamBufferConsumer();
            var fileBufferConsumer =
                new AggregatedFileStreamBufferConsumer(new IFileStreamBufferConsumer[] {tempFileWriter, shaComputer});
            await FileStreamUtils.ReadAsync(_inputFileStream, _inputOffset, _byteCount, fileBufferConsumer);
            return shaComputer.GetHash();
        }

        private async Task WriteTempFileAsync()
        {
            _console.Info($"writing data to {_tempFilePath}");
            await using var tempFileWriter = new WriteFileFileStreamBufferConsumer(_tempFilePath);
            await FileStreamUtils.ReadAsync(_inputFileStream, _inputOffset, _byteCount, tempFileWriter);
        }
    }
}
