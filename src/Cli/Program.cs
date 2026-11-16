using Cli;
using Core;
using Core.Abstractions;
using Core.Services;
using Core.Storage;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// Composition root: єдине місце, де створюються конкретні сховища і сервіс.
bool useFile = args.Contains("--file");
string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "catalog.json");

ICatalogStore store;
if (args.Contains("--factory"))
{
    // те саме, але через фабрику (додаткове завдання 3)
    store = StoreFactory.Create(args, dataPath);
}
else
{
    store = useFile
        ? new FileCatalogStore(dataPath)
        : new InMemoryCatalogStore(SampleData.Products());

    if (args.Contains("--cache"))
        store = new CachingCatalogStore(store);
}

var service = new CatalogService(store);

// Команда — перший аргумент без "--". Без команди запускається сценарій каталогу.
string command = args.FirstOrDefault(a => !a.StartsWith("--")) ?? "catalog";

return command switch
{
    "catalog" => CatalogCommand.Run(service, store, dataPath),
    "import" => ImportCommand.Run(args.Skip(1).ToArray()),
    "demo" => DomainDemo.Run(),
    "env" => EnvironmentPrinter.Print(args.Contains("--json")),
    _ => ImportCommand.Run(args)      // інакше це шлях до файлу для імпорту
};
