using System.IO;

namespace Nier.VeraCrypt.Tools
{
    public class CmdLineOptions
    {
        public string Password { get; set; }

        public FileInfo InputFile { get; set; }

        public FileInfo OutputFile { get; set; }

        public bool Verbose { get; set; }
    }
}
