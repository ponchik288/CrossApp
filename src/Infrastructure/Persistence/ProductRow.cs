namespace Infrastructure.Persistence;

// Рядок таблиці Products. Це модель зберігання, а не доменна сутність: тут звичайні сетери, які потрібні EF Core.
public sealed class ProductRow
{
    public string Id { get; set; } = "";
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";
    public string Unit { get; set; } = "";
    public int Quantity { get; set; }

    public List<BatchRow> Batches { get; set; } = [];
}
