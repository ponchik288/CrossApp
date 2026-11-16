namespace Core.Events;

// Дані події: який товар, що сталося, на скільки змінилася кількість і скільки стало.
public sealed class CatalogChangedEventArgs(
    string id, CatalogChangeKind kind, int delta, int quantityAfter) : EventArgs
{
    public string Id { get; } = id;
    public CatalogChangeKind Kind { get; } = kind;
    public int Delta { get; } = delta;
    public int QuantityAfter { get; } = quantityAfter;
    public DateTimeOffset At { get; } = DateTimeOffset.Now;
}
