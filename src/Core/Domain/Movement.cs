namespace Core.Domain;

public enum MovementStatus
{
    Draft,
    Confirmed,
    Cancelled
}

// Документ переміщення товару. Має явний стан і дозволені переходи між станами.
public sealed class Movement
{
    public string Id { get; }
    public string Sku { get; }
    public int Amount { get; }
    public MovementStatus Status { get; private set; }

    private Movement(string id, string sku, int amount)
    {
        Id = id;
        Sku = sku;
        Amount = amount;
        Status = MovementStatus.Draft;
    }

    public static Movement Create(string id, string sku, int amount)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Номер документа обов'язковий", nameof(id));

        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU не може бути порожнім", nameof(sku));

        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), amount,
                "Кількість у переміщенні має бути більшою за нуль");

        return new Movement(id.Trim(), sku.Trim().ToUpperInvariant(), amount);
    }

    public void Confirm() => ChangeStatus(MovementStatus.Confirmed);

    public void Cancel() => ChangeStatus(MovementStatus.Cancelled);

    private void ChangeStatus(MovementStatus next)
    {
        if (!CanChange(Status, next))
            throw new InvalidOperationException(
                $"Переміщення {Id}: перехід {Status} → {next} заборонений");

        Status = next;
    }

    // Дозволені переходи: чернетку можна підтвердити або скасувати, далі документ не змінюється.
    private static bool CanChange(MovementStatus from, MovementStatus to) => (from, to) switch
    {
        (MovementStatus.Draft, MovementStatus.Confirmed) => true,
        (MovementStatus.Draft, MovementStatus.Cancelled) => true,
        _ => false
    };

    public override string ToString() => $"{Id}: {Sku} × {Amount} [{Status}]";
}
