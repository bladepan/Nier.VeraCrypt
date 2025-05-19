using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Nier.VeraCrypt.Tools
{
    class Program
    {
        private int Run(CmdLineOptions options, IConsoleWrapper console)
        {
            switch (options.Mode)
            {
                case Mode.Default:
                    return ExecDefaultMode(options, console);
                case Mode.VerifyHeader:
                    return ExecVerifyVolumeMode(options, console);
                default:
                    console.Error($"Invalid mode {options.Mode}");
                    return 1;
            }
        }

        private int ExecDefaultMode(CmdLineOptions options, IConsoleWrapper console)
        {
            var password = options.Password;
            var filePath = options.InputFile.FullName;
            if (options.OutputFile == null)
            {
                throw new ArgumentException("OutputFile is required in Default mode.");
            }

            var outputFileStream = new FileStream(options.OutputFile.FullName, FileMode.Create, FileAccess.Write);
            console.Verbose($"Input file {options.InputFile}, output file {options.OutputFile}");
            VeraCryptVolume v = new(filePath, password);
            PrintVolumeHeaderInfo(v, console);
            Stopwatch stopwatch = Stopwatch.StartNew();
            v.ReadDataBytes(outputFileStream, 0,
                v.MasterKeyEncryptionSize, bytesRead =>
                {
                    if (bytesRead == 0 || stopwatch.Elapsed >= TimeSpan.FromSeconds(2) ||
                        bytesRead == v.MasterKeyEncryptionSize)
                    {
                        stopwatch.Restart();
                        console.Verbose($"Read {bytesRead} bytes.");
                    }
                });
            return 0;
        }

        private static void PrintVolumeHeaderInfo(VeraCryptVolume v, IConsoleWrapper console)
        {
            console.Verbose("magic " + v.Magic);
            console.Verbose("header version " + v.VolumeHeaderVersion);
            console.Verbose("min program Version " + v.MinProgramVersion);
            console.Verbose("volume size " + v.VolumeSize);
            console.Verbose("master key scope offset " + v.MasterKeyScopeOffset);
            console.Verbose("sector size " + v.SectorSize);
            console.Verbose("master key encryption size " + v.MasterKeyEncryptionSize);
        }

        private int ExecVerifyVolumeMode(CmdLineOptions options, IConsoleWrapper console)
        {
            var password = options.Password;
            var filePath = options.InputFile.FullName;
            console.Verbose($"Verify {filePath}");
            try
            {
                VeraCryptVolume v = new(filePath, password);
                PrintVolumeHeaderInfo(v, console);
                return 0;
            }
            catch (Exception ex)
            {
                console.Error($"Failed to verify volume: {ex.Message}");
                return 1;
            }
        }

        // encryption algorithm aes
        // hash algorithm sha-512
        // volume size 64MB
        // password test1
        // filesystem ext4

        // aes, block size 16, key size 32
        // xts mode The size of each data unit is always 512 bytes (regardless of the sector size).
        static Task<int> Main(string[] args)
        {
            var cmd = new RootCommand("decrypt and dump the volume data to output file")
            {
                new Option<string>("--password", "volume password") { IsRequired = true },
                new Option<FileInfo>("--inputFile", "input file") { IsRequired = true }.ExistingOnly(),
                new Option<FileInfo>("--outputFile", "output file"),
                new Option<bool>("--verbose", "enable more logging"),
                new Option<Mode>("--mode",
                    "default to dump the volume data, VerifyHeader to verify the volume header with password only")
            };
            cmd.Handler = CommandHandler.Create((CmdLineOptions options, IConsole console) =>
            {
                var consoleWrapper = new CommandLineConsoleWrapper(console, options.Verbose);
                var p = new Program();
                return p.Run(options, consoleWrapper);
            });

            return cmd.InvokeAsync(args);
        }
    }
}
