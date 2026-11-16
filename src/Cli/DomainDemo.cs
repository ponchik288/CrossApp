using Core.Domain;
using Core.Dto;
using Core.Import;

namespace Cli;

// Демонстрація доменної моделі (ЛР4): успішний сценарій і порушення інваріантів.
internal static class DomainDemo
{
    public static int Run()
    {
        Console.WriteLine("=== Сценарій 1: успіх ===");
        Product product = Product.Create("P-001", "sku-001", "Цемент М400 25кг", "шт", 100);
        Console.WriteLine(product);
        product.RegisterArrival(50);
        product.Issue(30);
        Console.WriteLine(product);

        Warehouse warehouse = Warehouse.Create("W-01", "Головний склад");
        warehouse.AddProduct(product);
        Console.WriteLine(warehouse);

        Console.WriteLine();
        Console.WriteLine("=== Сценарій 2: порушення інваріантів ===");
        TryDo("видача більша за залишок", () => product.Issue(1000));
        TryDo("порожній SKU", () => Product.Create("P-002", " ", "Пісок", "т", 10));
        TryDo("від'ємний залишок", () => Product.Create("P-003", "SKU-003", "Цегла", "шт", -5));
        TryDo("нульовий прихід", () => product.RegisterArrival(0));
        TryDo("дубль SKU на складі", () =>
            warehouse.AddProduct(Product.Create("P-009", "SKU-001", "Цемент (дубль)", "шт", 1)));
        Console.WriteLine($"  Після всіх відмов стан не змінився: {product}");

        Console.WriteLine();
        Console.WriteLine("=== Сценарій 3: правило на дві сутності (переміщення) ===");
        Product source = Product.Create("P-010", "SKU-010", "Клей плитковий", "кг", 540);
        Product target = Product.Create("P-110", "SKU-010", "Клей плитковий", "кг", 0);
        Product other = Product.Create("P-011", "SKU-011", "Гіпсокартон 12.5мм", "лист", 20);
        StockTransfer.Move(source, target, 100);
        Console.WriteLine($"  {source}");
        Console.WriteLine($"  {target}");
        TryDo("переміщення між різними товарами", () => StockTransfer.Move(source, other, 10));
        TryDo("переміщення понад залишок", () => StockTransfer.Move(source, target, 5000));

        Console.WriteLine();
        Console.WriteLine("=== Сценарій 4: стан документа переміщення ===");
        Movement movement = Movement.Create("M-001", "SKU-010", 100);
        Console.WriteLine($"  {movement}");
        movement.Confirm();
        Console.WriteLine($"  {movement}");
        TryDo("скасувати підтверджене", () => movement.Cancel());
        TryDo("підтвердити вдруге", () => movement.Confirm());

        Console.WriteLine();
        Console.WriteLine("=== Сценарій 5: сутності з результату імпорту ===");
        string path = CliSettings.Sample("stock.csv");
        if (!File.Exists(path))
        {
            Console.WriteLine($"  Файл не знайдено: {Path.GetFullPath(path)}");
            return 1;
        }

        ImportResult<Product> imported = ProductFactory.FromImport(ProductCsvImporter.Load(path));
        foreach (Product p in imported.Items)
            Console.WriteLine($"  {p}");
        ImportCommand.PrintErrors(imported.Errors);

        return 0;
    }

    private static void TryDo(string title, Action action)
    {
        try
        {
            action();
            Console.WriteLine($"  {title}: виняток НЕ спрацював — інваріант відсутній!");
        }
        catch (Exception ex)
        {
            // Повідомлення ArgumentOutOfRangeException займає два рядки, зводимо до одного.
            Console.WriteLine($"  {title}: {ex.GetType().Name} — {ex.Message.ReplaceLineEndings(" ")}");
        }
    }
}
