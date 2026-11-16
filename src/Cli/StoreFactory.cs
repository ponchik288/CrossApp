using Core;
using Core.Abstractions;
using Core.Storage;

namespace Cli;

// Альтернатива до вибору сховища прямо в Program.cs (додаткове завдання 3).
internal static class StoreFactory
{
    public static ICatalogStore Create(string[] args, string dataPath)
    {
        ICatalogStore store = args.Contains("--file")
            ? new FileCatalogStore(dataPath)
            : new InMemoryCatalogStore(SampleData.Products());

        return args.Contains("--cache") ? new CachingCatalogStore(store) : store;
    }
}
