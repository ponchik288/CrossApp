using Core.Domain;

namespace Core.Reports;

// Усі звіти. Методи приймають послідовність товарів, а не сховище, і повертають DTO.
// Кожен метод закінчується ToList(), тому запит виконується тут, один раз.
public static class ReportService
{
    // 1) фільтр + сортування + проєкція: товари із залишком менше порога
    public static IReadOnlyList<StockLine> LowStock(IEnumerable<Product> source, int threshold) =>
        source.Where(p => p.Quantity < threshold)
              .OrderBy(p => p.Quantity).ThenBy(p => p.Sku, StringComparer.Ordinal)
              .Select(p => new StockLine(p.Sku, p.Name, p.Quantity, p.Unit))
              .ToList();

    // 2) топ-N зі стабільним порядком: другий ключ (SKU) розводить однакові залишки
    public static IReadOnlyList<StockLine> TopByQuantity(IEnumerable<Product> source, int n) =>
        source.OrderByDescending(p => p.Quantity).ThenBy(p => p.Sku, StringComparer.Ordinal)
              .Take(n)
              .Select(p => new StockLine(p.Sku, p.Name, p.Quantity, p.Unit))
              .ToList();

    // 3) групування за одиницею виміру з підрахунком і сумою
    public static IReadOnlyList<GroupSummary> ByUnit(IEnumerable<Product> source) =>
        source.GroupBy(p => p.Unit, StringComparer.OrdinalIgnoreCase)
              .Select(g => new GroupSummary(g.Key, g.Count(), g.Sum(p => p.Quantity)))
              .OrderByDescending(s => s.TotalQuantity).ThenBy(s => s.Key, StringComparer.Ordinal)
              .ToList();

    // 4) агрегати з обробкою порожньої послідовності
    public static (int Items, int Total, double Average, int Max) Totals(IEnumerable<Product> source)
    {
        var list = source.ToList();

        return list.Count == 0
            ? (0, 0, 0d, 0)
            : (list.Count, list.Sum(p => p.Quantity), list.Average(p => p.Quantity), list.Max(p => p.Quantity));
    }

    // Те саме групування в синтаксисі запитів (додаткове завдання 3).
    public static IReadOnlyList<GroupSummary> ByUnitQuerySyntax(IEnumerable<Product> source) =>
        (from product in source
         group product by product.Unit.ToLowerInvariant() into g
         orderby g.Sum(p => p.Quantity) descending, g.Key
         select new GroupSummary(g.Key, g.Count(), g.Sum(p => p.Quantity)))
        .ToList();

    // Найбільші групи: власний метод розширення TopBy (додаткове завдання 2).
    public static IReadOnlyList<GroupSummary> TopGroups(IEnumerable<Product> source, int n) =>
        ByUnit(source).TopBy(g => g.TotalQuantity, n).ToList();

    // GroupJoin двох колекцій: товари × переміщення (додаткове завдання 1).
    // GroupJoin залишає і ті товари, для яких переміщень немає.
    public static IReadOnlyList<MovementSummary> MovementsByProduct(
        IEnumerable<Product> products, IEnumerable<Movement> movements) =>
        products.GroupJoin(
                    movements,
                    product => product.Sku,
                    movement => movement.Sku,
                    (product, group) => new MovementSummary(
                        product.Sku, product.Name, group.Count(), group.Sum(m => m.Amount)))
                .OrderBy(s => s.Sku, StringComparer.Ordinal)
                .ToList();

    // Товари, які переміщували найбільше: теж через TopBy.
    public static IReadOnlyList<MovementSummary> MostMoved(
        IEnumerable<Product> products, IEnumerable<Movement> movements, int n) =>
        MovementsByProduct(products, movements).TopBy(s => s.TotalAmount, n).ToList();

    // Варіант LowStock із зайвими ToList посередині ланцюжка — лише для порівняння швидкості.
    public static IReadOnlyList<StockLine> LowStockWithExtraLists(IEnumerable<Product> source, int threshold)
    {
        var filtered = source.Where(p => p.Quantity < threshold).ToList();
        var ordered = filtered.OrderBy(p => p.Quantity).ThenBy(p => p.Sku, StringComparer.Ordinal).ToList();
        return ordered.Select(p => new StockLine(p.Sku, p.Name, p.Quantity, p.Unit)).ToList();
    }
}
