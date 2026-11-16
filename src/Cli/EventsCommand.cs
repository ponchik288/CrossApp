using Core;
using Core.Domain;
using Core.Events;
using Core.Services;

namespace Cli;

// Сценарій ЛР6: підписка на подію сервісу, другий підписник, відписка, лог у файл.
internal static class EventsCommand
{
    public static string Format(CatalogChangedEventArgs e) =>
        $"{e.At:HH:mm:ss} {e.Kind,-8} {e.Id} {e.Delta,4:+0;-0} → залишок {e.QuantityAfter}";

    public static int Run(CatalogService service, string logPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

        // Перший підписник — локальний метод: друкує в консоль і дописує у файл.
        void OnCatalogChanged(object? sender, CatalogChangedEventArgs e)
        {
            string line = Format(e);
            Console.WriteLine(line);
            File.AppendAllText(logPath, line + Environment.NewLine);
        }

        service.Changed += OnCatalogChanged;

        // Другий підписник — лямбда із замиканням над локальною змінною changes.
        int changes = 0;
        service.Changed += (_, _) => changes++;

        Product created = service.Add("SKU-101", "Кабель UTP cat6", "м", 50);
        service.Receive(created.Id, 25);
        service.Issue(created.Id, 10);
        Console.WriteLine($"Подій отримано: {changes}");

        // Сценарій відмови: є текст помилки, рядка події немає.
        Console.WriteLine();
        Console.WriteLine("Відмови (події не виникають):");
        TryDo("прихід з від'ємною кількістю", () => service.Receive(created.Id, -5));
        TryDo("прихід для неіснуючого id", () => service.Receive("нема", 5));
        Console.WriteLine($"Подій отримано: {changes}");

        // Відписка: операція виконується, але перший підписник мовчить.
        Console.WriteLine();
        Console.WriteLine("Після відписки OnCatalogChanged:");
        service.Changed -= OnCatalogChanged;
        service.Receive(created.Id, 5);
        Console.WriteLine($"Подій отримано: {changes}");
        Console.WriteLine($"Стан товару: {service.Find(created.Id)}");

        // Func замість події: тут потрібне значення у відповідь, а не сповіщення.
        Console.WriteLine();
        IReadOnlyList<Product> low = service.FindAll(p => p.Quantity < 10);
        Console.WriteLine($"FindAll(p => p.Quantity < 10): {low.Count}");
        foreach (Product p in low)
            Console.WriteLine($"  {p}");

        Console.WriteLine();
        Console.WriteLine($"Лог: {logPath}");
        foreach (string line in File.ReadLines(logPath).TakeLast(4))
            Console.WriteLine($"  {line}");

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
