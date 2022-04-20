using System.IO;

namespace Nier.VeraCrypt.Tools
{
    public enum Mode
    {
        Default,
        // verify the header only
        VerifyHeader
    }

    public class CmdLineOptions
    {
        public string Password { get; set; }

        public FileInfo InputFile { get; set; }

        public FileInfo OutputFile { get; set; }

        public bool Verbose { get; set; }

        public Mode Mode { get; set; }
    }
}
