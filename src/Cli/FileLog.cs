namespace Cli;

// Простий лог у файл з мінімальним рівнем із конфігурації (Logging:MinimumLevel).
internal sealed class FileLog(string path, string minimumLevel)
{
    private static readonly string[] Levels = ["Debug", "Information", "Warning", "Error"];

    private readonly int _minimum = Math.Max(0, Array.FindIndex(Levels, l => l.Equals(minimumLevel, StringComparison.OrdinalIgnoreCase)));

    public void Debug(string message) => Write(0, message);
    public void Info(string message) => Write(1, message);
    public void Warning(string message) => Write(2, message);

    private void Write(int level, string message)
    {
        if (level < _minimum)
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.AppendAllText(path, $"[{Levels[level]}] {message}{Environment.NewLine}");
    }
}
