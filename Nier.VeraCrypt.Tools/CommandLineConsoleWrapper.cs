using System;
using System.CommandLine;
using System.CommandLine.IO;

namespace Nier.VeraCrypt.Tools
{
    public class CommandLineConsoleWrapper: IConsoleWrapper
    {
        private readonly IConsole _console;
        private readonly bool _verbose;
        public CommandLineConsoleWrapper(IConsole console, bool verbose)
        {
            _console = console;
            _verbose = verbose;
        }

        public void Verbose(string msg)
        {
            if (_verbose)
            {
                _console.Out.WriteLine($"{DateTimeOffset.Now:O} {msg}");
            }
        }
    }
}
