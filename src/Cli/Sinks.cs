using Core.Abstractions;
using Core.Events;

namespace Cli;

// Одержувачі сповіщень для варіанту «через конструктор» (додаткове завдання ЛР6).
internal sealed class ConsoleSink : INotificationSink
{
    public void Notify(CatalogChangedEventArgs change) =>
        Console.WriteLine($"  [sink → консоль] {EventsCommand.Format(change)}");
}

internal sealed class FileSink(string path) : INotificationSink
{
    public void Notify(CatalogChangedEventArgs change)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.AppendAllText(path, $"[sink] {EventsCommand.Format(change)}{Environment.NewLine}");
    }
}
