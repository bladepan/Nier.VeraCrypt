using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Nier.FileTools.Backup
{
    public class BackupWorker
    {
        private readonly int _chunkCount;
        private readonly int _chunkSize;
        private readonly FileInfo _inputFileInfo;
        private readonly FileInfo _outDirInfo;
        private readonly FileInfo _tempDirInfo;
        private readonly IConsoleWrapper _console;

        public BackupWorker(CmdLineOptions options, IConsoleWrapper consoleWrapper)
        {
            if (options.ChunkSize <= 0)
            {
                throw new ArgumentException("Invalid chunk size");
            }

            long inputFileSize = options.InputFile.Length;
            long chunkCount = inputFileSize / options.ChunkSize + 1;

            if (chunkCount > 2048)
            {
                throw new ArgumentException(
                    $"Chunk count {chunkCount} exceeds limit 2048. Consider increase chunk size.");
            }

            _chunkCount = (int)chunkCount;
            _chunkSize = options.ChunkSize;
            _inputFileInfo = options.InputFile;
            _outDirInfo = options.OutputDir;
            _tempDirInfo = options.TempDir;
            _console = consoleWrapper;
        }

        public async Task<ChunkWriterResult[]> RunAsync()
        {
            await using var inputStream = File.OpenRead(_inputFileInfo.FullName);
            var workers = Enumerable.Range(0, _chunkCount).Select(i => new ChunkWriter(new ChunkWriterOptions
            {
                InputFileStream = inputStream,
                OutputDir = _outDirInfo,
                ChunkSize = _chunkSize,
                ChunkIndex = i,
                TempDir = _tempDirInfo,
                Console = _console
            })).ToImmutableList();
            return await Task.WhenAll(workers.Select(w => w.WriteChunk()));
        }
    }
}
