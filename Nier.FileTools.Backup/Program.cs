using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;

namespace Nier.FileTools.Backup
{
    class Program
    {
        public void Run(CmdLineOptions options)
        {
            // TODO

        }

        static Task<int> Main(string[] args)
        {
            var cmd = new RootCommand("split a large file to smaller chunks")
            {
                new Option<FileInfo>("--inputFile", "input file") {IsRequired = true}.ExistingOnly(),
                new Option<FileInfo>("--outputDir", "output directory") {IsRequired = true},
                new Option<int>("--chunkSize"),
                new Option<bool>("--verbose", "enable more logging")
            };

            cmd.Handler = CommandHandler.Create((CmdLineOptions options, IConsole console) =>
            {
                var p = new Program();
                p.Run(options);
            });

            return cmd.InvokeAsync(args);
        }
    }
}
