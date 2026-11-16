namespace Core.Tests;

// Тимчасовий файл для тестів: створюється в системному temp і видаляється в Dispose (using).
internal sealed class TempFile : IDisposable
{
    public string Path { get; }

    public TempFile(string extension, string? content = null)
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"crossapp-test-{Guid.NewGuid():N}{extension}");
        if (content is not null)
            File.WriteAllText(Path, content);
    }

    public void Dispose() => File.Delete(Path);
}
