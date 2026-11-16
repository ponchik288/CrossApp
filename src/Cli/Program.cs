using Cli;
using Core;
using Core.Abstractions;
using Core.Services;
using Core.Storage;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// Конфігурація: appsettings.json, файл середовища, user-secrets, змінні CROSSAPP_*, аргументи.
CliSettings settings = CliSettings.Load(args);
var log = new FileLog(settings.LogFile, settings.Options.Logging.MinimumLevel);
log.Debug($"запуск: середовище {settings.EnvironmentName}, аргументи: {string.Join(' ', args)}");
log.Debug($"файл даних: {settings.DataFile}");

// Composition root: єдине місце, де створюються конкретні сховища і сервіс.
bool useFile = args.Contains("--file");
string dataPath = settings.DataFile;

ICatalogStore store;
if (args.Contains("--factory"))
{
    // те саме, але через фабрику (додаткове завдання ЛР5)
    store = StoreFactory.Create(args, dataPath);
}
else
{
    store = useFile
        ? new FileCatalogStore(dataPath)
        : new InMemoryCatalogStore(args.Contains("--empty") ? null : SampleData.Products());

    if (args.Contains("--cache"))
        store = new CachingCatalogStore(store);
}

string logPath = settings.LogFile;

// Одержувач сповіщень через конструктор (додаткове завдання ЛР6); без прапорця його немає.
INotificationSink? sink =
    args.Contains("--sink-file") ? new FileSink(logPath) :
    args.Contains("--sink") ? new ConsoleSink() :
    null;

var service = new CatalogService(store, sink);

// Команда — перший аргумент без "--". Без команди запускається сценарій каталогу.
string command = args.FirstOrDefault(a => !a.StartsWith("--")) ?? "catalog";

try
{
    if (command == "import-async")
        return await ImportAsyncCommand.RunAsync(service, args);

    return command switch
    {
        "catalog" => CatalogCommand.Run(service, store, dataPath),
        "events" => EventsCommand.Run(service, logPath, log),
        "config" => DataCommands.Config(settings),
        "export" => DataCommands.Export(settings, service, args, useFile),
        "add" => DataCommands.Add(settings, service, args),
        _ when command.StartsWith("report") => ReportCommand.Run(command, service),
        "import" => ImportCommand.Run(args.Skip(1).ToArray()),
        "demo" => DomainDemo.Run(),
        "env" => EnvironmentPrinter.Print(args.Contains("--json")),
        _ => ImportCommand.Run(args)      // інакше це шлях до файлу для імпорту
    };
}
catch (Exception ex) when (ex is InvalidDataException or DomainException or ArgumentException)
{
    // Зрозумілий текст замість stack trace: зіпсований файл даних або порушене правило.
    log.Warning(ex.Message);
    Console.WriteLine($"Помилка: {ex.Message.ReplaceLineEndings(" ")}");
    return 5;
}
