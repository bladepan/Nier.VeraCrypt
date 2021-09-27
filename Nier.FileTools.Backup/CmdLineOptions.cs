using System.IO;

namespace Nier.FileTools.Backup
{
    public class CmdLineOptions
    {
        public FileInfo InputFile { get; set; }

        public FileInfo OutputDir { get; set; }

        public FileInfo TempDir { get; set; }

        public int ChunkSize { get; set; } = 64 * 1024 * 1024;
    }
}
