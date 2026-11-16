using System.Diagnostics;
using Core;
using Core.Domain;
using Core.Reports;
using Core.Services;

namespace Cli;

// Підкоманди звітів (ЛР7). Тут лише вибір звіту і вивід; усі запити LINQ — у Core/Reports.
internal static class ReportCommand
{
    public static int Run(string command, CatalogService service)
    {
        IReadOnlyList<Product> items = service.All();

        switch (command)
        {
            case "report-top":
                Print(ReportService.TopByQuantity(items, 5));
                break;

            case "report-low":
                Print(ReportService.LowStock(items, 10));
                break;

            case "report-groups":
                PrintGroups(ReportService.ByUnit(items));
                break;

            case "report-groups-query":
                PrintGroups(ReportService.ByUnitQuerySyntax(items));
                break;

            case "report-top-groups":
                PrintGroups(ReportService.TopGroups(items, 3));
                break;

            case "report-sum":
                var t = ReportService.Totals(items);
                Console.WriteLine($"  позицій {t.Items}, всього {t.Total}, середнє {t.Average:F2}, макс {t.Max}");
                break;

            case "report-moves":
                PrintMoves(ReportService.MovementsByProduct(items, SampleData.Movements()));
                break;

            case "report-most-moved":
                PrintMoves(ReportService.MostMoved(items, SampleData.Movements(), 3));
                break;

            case "report-deferred":
                DeferredDemo(service);
                break;

            case "report-bench":
                Benchmark();
                break;

            default:
                Console.WriteLine("report-top | report-low | report-groups | report-sum | report-deferred");
                Console.WriteLine("report-groups-query | report-top-groups | report-moves | report-most-moved | report-bench");
                return 2;
        }

        return 0;
    }

    private static void Print(IReadOnlyList<StockLine> lines)
    {
        if (lines.Count == 0)
            Console.WriteLine("  (немає записів)");

        foreach (StockLine l in lines)
            Console.WriteLine($"  {l.Sku,-10} {l.Name,-32} {l.Quantity,6} {l.Unit}");
    }

    private static void PrintGroups(IReadOnlyList<GroupSummary> groups)
    {
        if (groups.Count == 0)
            Console.WriteLine("  (немає записів)");

        foreach (GroupSummary g in groups)
            Console.WriteLine($"  {g.Key,-6} позицій: {g.Count,3}   сумарно: {g.TotalQuantity,6}");
    }

    private static void PrintMoves(IReadOnlyList<MovementSummary> lines)
    {
        foreach (MovementSummary m in lines)
            Console.WriteLine($"  {m.Sku,-10} {m.Name,-32} переміщень: {m.Movements,2}   кількість: {m.TotalAmount,5}");
    }

    // Відкладене виконання: запит виконується не там, де його написано, а там, де його перебирають.
    private static void DeferredDemo(CatalogService service)
    {
        // service.All() щоразу повертає новий знімок, тому для демо беремо локальний список.
        List<Product> source = service.All().ToList();

        IEnumerable<Product> query = service.Filter(source, p => p.Quantity < 10);   // запит побудовано, не виконано
        Console.WriteLine($"Дефіцитних до змін: {query.Count()}");

        source.Add(Product.Create("tmp00001", "SKU-999", "Скоба", "шт", 3));
        Console.WriteLine($"Дефіцитних після змін: {query.Count()}");               // число інше

        List<Product> snapshot = query.ToList();                                     // результат зафіксовано
        source.Clear();
        Console.WriteLine($"У знімку лишилось: {snapshot.Count}");                   // не змінюється
        Console.WriteLine($"Запит по очищеному джерелу: {query.Count()}");
    }

    // Час звіту на 100 000 згенерованих записів: з ToList посередині ланцюжка і без нього.
    private static void Benchmark()
    {
        var random = new Random(42);
        var data = new List<Product>(100_000);
        for (int i = 0; i < 100_000; i++)
            data.Add(Product.Create($"G-{i:D6}", $"SKU-G{i:D6}", $"Товар {i}", "шт", random.Next(0, 1000)));

        // перший прогін не враховуємо: у ньому JIT компілює методи
        ReportService.LowStock(data, 500);
        ReportService.LowStockWithExtraLists(data, 500);

        const int runs = 20;
        double lazy = Measure(() => ReportService.LowStock(data, 500), runs);
        double eager = Measure(() => ReportService.LowStockWithExtraLists(data, 500), runs);

        Console.WriteLine($"  записів: {data.Count}, прогонів: {runs}");
        Console.WriteLine($"  без проміжних ToList: {lazy:F2} мс у середньому");
        Console.WriteLine($"  з ToList посередині:  {eager:F2} мс у середньому");
    }

    private static double Measure(Action action, int runs)
    {
        var watch = Stopwatch.StartNew();
        for (int i = 0; i < runs; i++)
            action();
        watch.Stop();

        return watch.Elapsed.TotalMilliseconds / runs;
    }
}
