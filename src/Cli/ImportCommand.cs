using Core.Dto;
using Core.Import;

namespace Cli;

// Імпорт товарів із файлу (ЛР3): аргументи, виклик Core, вивід.
internal static class ImportCommand
{
    public static int Run(string[] args)
    {
        // Шлях до файлу — перший аргумент, інакше зразковий sample.csv поруч із застосунком.
        string path = args.Length > 0 ? args[0] : CliSettings.Sample("sample.csv");

        if (!File.Exists(path))
        {
            Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
            return 1;
        }

        // Змішаний файл (товари і склади) читає окремий імпортер.
        if (args.Contains("--mixed"))
        {
            PrintMixed(MixedCsvImporter.Load(path));
            return 0;
        }

        // Імпортер обираємо за розширенням файлу.
        ImportResult<ProductDto>? result = Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".csv" => args.Contains("--trace")
                ? ProductCsvImporter.Load(path, e => Console.WriteLine($"  [пропуск під час читання] {e}"))
                : ProductCsvImporter.Load(path),
            ".json" => ProductJsonImporter.Load(path),
            _ => null
        };

        if (result is null)
        {
            Console.WriteLine($"Непідтримуваний формат файлу: {Path.GetExtension(path)} (потрібен .csv або .json)");
            return 2;
        }

        Console.WriteLine($"Завантажено записів: {result.Items.Count}");
        foreach (ProductDto p in result.Items.Take(5))
            Console.WriteLine($"  {p.Id,-6} {p.Sku,-10} {p.Name,-32} {p.Quantity,5} {p.Unit}");

        PrintErrors(result.Errors);

        Console.WriteLine(
            $"Статистика: усього {result.Total} / прийнято {result.Items.Count} / " +
            $"пропущено {result.Errors.Count} / помилок {result.ErrorPercent:F1}%");

        return 0;
    }

    private static void PrintMixed(MixedImportResult mixed)
    {
        Console.WriteLine($"Товарів: {mixed.Products.Count}");
        foreach (ProductDto p in mixed.Products)
            Console.WriteLine($"  {p.Id,-6} {p.Sku,-10} {p.Name,-32} {p.Quantity,5} {p.Unit}");

        Console.WriteLine($"Складів: {mixed.Warehouses.Count}");
        foreach (WarehouseDto w in mixed.Warehouses)
            Console.WriteLine($"  {w.Id,-6} {w.Name,-24} {w.Address ?? "(адреса не вказана)"}");

        PrintErrors(mixed.Errors);
    }

    internal static void PrintErrors(IReadOnlyList<string> errors)
    {
        if (errors.Count == 0)
            return;

        Console.WriteLine($"Пропущено рядків: {errors.Count}");
        foreach (string e in errors)
            Console.WriteLine($"  ! {e}");
    }
}
