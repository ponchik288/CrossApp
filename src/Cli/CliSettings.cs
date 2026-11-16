using System.Reflection;
using Core.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cli;

// Усі налаштування запуску в одному місці: зібрана конфігурація і готові шляхи.
internal sealed class CliSettings
{
    public required string EnvironmentName { get; init; }
    public required AppOptions Options { get; init; }
    public required string DataFile { get; init; }
    public required string LogFile { get; init; }
    public string? ApiKey { get; init; }

    public static CliSettings Load(string[] args)
    {
        string envName = Environment.GetEnvironmentVariable("CROSSAPP_ENVIRONMENT") ?? "Development";

        // Пізніше джерело перекриває попереднє: файл < файл середовища < user-secrets < змінні середовища < аргументи.
        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{envName}.json", optional: true)
            .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
            .AddEnvironmentVariables(prefix: "CROSSAPP_")
            .AddCommandLine(ConfigArguments(args))
            .Build();

        AppOptions options = config.Get<AppOptions>() ?? new AppOptions();

        // Проста змінна CROSSAPP_DATA_PATH перекриває розділ Storage.
        string? legacy = Environment.GetEnvironmentVariable("CROSSAPP_DATA_PATH");
        var storage = new StorageOptions
        {
            DataPath = string.IsNullOrWhiteSpace(legacy) ? options.Storage.DataPath : legacy,
            FileName = options.Storage.FileName,
            UseLocalAppData = options.Storage.UseLocalAppData
        };

        // Базовий каталог для відносного DataPath: каталог застосунку або локальні дані користувача.
        string baseDir = storage.UseLocalAppData
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CrossApp")
            : AppContext.BaseDirectory;

        return new CliSettings
        {
            EnvironmentName = envName,
            Options = options,
            DataFile = storage.ResolveFullPath(baseDir),
            LogFile = Path.Combine(baseDir, "logs", "app.log"),
            ApiKey = config["Integration:ApiKey"]
        };
    }

    // Зразкові файли (data/ з репозиторію) лежать поруч із застосунком у каталозі samples.
    public static string Sample(string fileName) => Path.Combine(AppContext.BaseDirectory, "samples", fileName);

    // У конфігурацію передаємо лише аргументи виду --Розділ:Ключ=значення; решта — команди і прапорці Cli.
    private static string[] ConfigArguments(string[] args) =>
        args.Where(a => a.StartsWith("--", StringComparison.Ordinal) && a.Contains(':') && a.Contains('=')).ToArray();

    public void PrintSummary()
    {
        Console.WriteLine($"Середовище : {EnvironmentName}");
        Console.WriteLine($"Файл даних : {DataFile}");
        Console.WriteLine($"Файл логу  : {LogFile} (рівень {Options.Logging.MinimumLevel})");
        Console.WriteLine($"Одиниця за замовчуванням : {Options.Warehouse.DefaultUnit}");
        // Секрет не друкуємо ніколи: лише факт наявності.
        Console.WriteLine(ApiKey is null ? "ApiKey: не заданий" : $"ApiKey: присутній, довжина {ApiKey.Length}");
    }
}
