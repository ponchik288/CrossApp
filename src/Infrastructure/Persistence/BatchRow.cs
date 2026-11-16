namespace Infrastructure.Persistence;

// Рядок таблиці Batches: одне надходження товару. Багато партій належать одному товару (зв'язок 1:N).
public sealed class BatchRow
{
    public int Id { get; set; }
    public string ProductId { get; set; } = "";
    public int Amount { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }

    public ProductRow Product { get; set; } = null!;
}
