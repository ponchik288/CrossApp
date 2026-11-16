using Core.Domain;
using Core.Services;
using Core.Storage;

namespace Cli;

// Команди ЛР9: показати конфігурацію, експортувати колекцію в JSON, додати товар.
internal static class DataCommands
{
    public static int Config(CliSettings settings)
    {
        settings.PrintSummary();
        return 0;
    }

    // Експорт усієї колекції в JSON. Без аргументу — у файл даних із конфігурації.
    public static int Export(CliSettings settings, CatalogService service, string[] args, bool storeIsFile)
    {
        string? target = args.Skip(1).FirstOrDefault(a => !a.StartsWith("--"));

        // Якщо сховище і так працює з файлом даних, експортуємо поруч, щоб не писати файл у самого себе.
        string path = target is not null
            ? Path.GetFullPath(target)
            : storeIsFile
                ? Path.Combine(Path.GetDirectoryName(settings.DataFile)!, "export.json")
                : settings.DataFile;

        settings.PrintSummary();

        IReadOnlyList<Product> items = service.All();
        CatalogJson.WriteAtomic(path, items);

        Console.WriteLine($"Експортовано записів: {items.Count}");
        Console.WriteLine($"Файл експорту: {path}");
        return 0;
    }

    // add <sku> <назва> <кількість> [одиниця] — одиниця за замовчуванням береться з Warehouse:DefaultUnit.
    public static int Add(CliSettings settings, CatalogService service, string[] args)
    {
        string[] values = args.Skip(1).Where(a => !a.StartsWith("--")).ToArray();

        if (values.Length < 3 || !int.TryParse(values[2], out int quantity))
        {
            Console.WriteLine("Використання: add <sku> <назва> <кількість> [одиниця]");
            return 2;
        }

        string unit = values.Length > 3 ? values[3] : settings.Options.Warehouse.DefaultUnit;
        Product created = service.Add(values[0], values[1], unit, quantity);
        Console.WriteLine($"Додано: {created}");
        return 0;
    }
}
