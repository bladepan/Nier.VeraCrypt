namespace Nier.VeraCrypt.Tools
{
    public interface IConsoleWrapper
    {
        void Verbose(string msg);
        void Error(string msg);
    }
}
