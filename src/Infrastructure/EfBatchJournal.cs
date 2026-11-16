using Core.Abstractions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

// Журнал надходжень у таблиці Batches.
public sealed class EfBatchJournal(AppDbContext db) : IBatchJournal
{
    public void Record(string productId, int amount)
    {
        db.Batches.Add(new BatchRow { ProductId = productId, Amount = amount, ReceivedAt = DateTimeOffset.Now });
        db.SaveChanges();
    }

    public IReadOnlyList<BatchRecord> ForProduct(string productId) =>
        db.Batches.AsNoTracking()
          .Where(b => b.ProductId == productId)
          .OrderBy(b => b.Id)
          .Select(b => new BatchRecord(b.Id, b.ProductId, b.Amount, b.ReceivedAt))
          .ToList();
}
