using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Services;
using Core.Storage;

namespace Cli;

// Сценарій роботи з каталогом (ЛР5). Отримує готовий сервіс: звідки дані, цей клас не знає.
internal static class CatalogCommand
{
    public static int Run(CatalogService service, ICatalogStore store, string dataPath)
    {
        string storeName = store is CachingCatalogStore ? store.ToString()! : store.GetType().Name;
        Console.WriteLine($"Сховище: {storeName}");
        if (storeName.Contains("File"))
            Console.WriteLine($"Файл: {dataPath}");

        Console.WriteLine($"Записів на початку: {service.All().Count}");

        // Додати, змінити, знайти.
        Product created = service.Add("SKU-101", "Кабель UTP cat6", "м", 50);
        Console.WriteLine($"Додано: {created}");

        service.Receive(created.Id, 25);
        service.Issue(created.Id, 10);
        Console.WriteLine($"Після приходу 25 і видачі 10: {service.Find(created.Id)}");

        Product? missing = service.Find("нема");
        Console.WriteLine($"Пошук за неіснуючим id: {(missing is null ? "не знайдено" : missing.ToString())}");

        Console.WriteLine();
        Console.WriteLine($"Усі записи ({service.All().Count}):");
        foreach (Product p in service.All())
            Console.WriteLine($"  {p.Id,-9} {p.Sku,-8} {p.Name,-32} {p.Quantity,5} {p.Unit}");

        // Пошук з умовою, яку передаємо лямбдою.
        Console.WriteLine();
        IReadOnlyList<Product> low = service.FindAll(p => p.Quantity < 20);
        Console.WriteLine($"Товари із залишком менше 20: {low.Count}");
        foreach (Product p in low)
            Console.WriteLine($"  {p}");

        // Сценарії відмови.
        Console.WriteLine();
        Console.WriteLine("Сценарії відмови:");
        TryDo("прихід для неіснуючого id", () => service.Receive("нема", 5));
        TryDo("видача понад залишок", () => service.Issue(created.Id, 100000));
        TryDo("дубль id у сховищі", () => store.Add(created));

        if (store is CachingCatalogStore cache)
        {
            Console.WriteLine();
            Console.WriteLine($"Кеш списку: з кешу {cache.Hits}, зі сховища {cache.Misses}");
        }

        return 0;
    }

    private static void TryDo(string title, Action action)
    {
        try
        {
            action();
            Console.WriteLine($"  {title}: помилки не було");
        }
        catch (Exception ex) when (ex is DomainException or InvalidOperationException or ArgumentException)
        {
            Console.WriteLine($"  {title}: {ex.Message.ReplaceLineEndings(" ")}");
        }
    }
}
