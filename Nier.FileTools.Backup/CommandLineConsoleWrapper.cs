using System;
using System.CommandLine;
using System.CommandLine.IO;

namespace Nier.FileTools.Backup
{
    public class CommandLineConsoleWrapper: IConsoleWrapper
    {
        private readonly IConsole _console;

        public CommandLineConsoleWrapper(IConsole console)
        {
            _console = console;
        }

        public void Info(string msg)
        {
            _console.Out.WriteLine($"{DateTimeOffset.Now:O} [INFO] {msg}");
        }
    }
}
