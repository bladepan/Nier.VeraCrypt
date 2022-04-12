using System;

namespace Nier.FileTools.Backup.Tests;

public class SysConsoleWrapper: IConsoleWrapper
{
    public void Info(string message) => Console.WriteLine($"{DateTime.Now} {message}");
}
