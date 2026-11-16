namespace Core.Abstractions;

// Журнал надходжень (партій). Записує окрему операцію «прихід»; звичайне оновлення товару партію не створює.
public interface IBatchJournal
{
    void Record(string productId, int amount);
    IReadOnlyList<BatchRecord> ForProduct(string productId);
}

public sealed record BatchRecord(int Id, string ProductId, int Amount, DateTimeOffset ReceivedAt);
