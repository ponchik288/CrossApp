namespace Core.Configuration;

// Налаштування застосунку. Значення за замовчуванням діють, якщо ключа немає в жодному джерелі.
public sealed class AppOptions
{
    public StorageOptions Storage { get; init; } = new();
    public LoggingOptions Logging { get; init; } = new();
    public WarehouseOptions Warehouse { get; init; } = new();
    public string EnvironmentName { get; init; } = "Development";
}

public sealed class StorageOptions
{
    public string DataPath { get; init; } = "data";
    public string FileName { get; init; } = "items.json";

    // true: дані лежать у каталозі локальних даних користувача, а не поруч із застосунком.
    public bool UseLocalAppData { get; init; }

    // Відносний DataPath рахуємо від baseDir; абсолютний використовуємо як є.
    public string ResolveFullPath(string baseDir)
    {
        string root = Path.IsPathRooted(DataPath) ? DataPath : Path.Combine(baseDir, DataPath);
        Directory.CreateDirectory(root);
        return Path.GetFullPath(Path.Combine(root, FileName));
    }
}

public sealed class LoggingOptions
{
    public string MinimumLevel { get; init; } = "Information";
}

public sealed class WarehouseOptions
{
    // Одиниця виміру, якщо під час додавання товару її не вказали.
    public string DefaultUnit { get; init; } = "шт";
}
