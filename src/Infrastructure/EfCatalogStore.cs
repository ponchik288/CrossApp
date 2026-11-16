using Core;
using Core.Abstractions;
using Core.Domain;
using Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

// Сховище на EF Core. Контракт ICatalogStore не змінювався: сервіс і маршрути не знають, що тепер це база даних.
// Синхронні виклики БД — свідоме навчальне спрощення для сумісності з інтерфейсом ЛР5.
public sealed class EfCatalogStore(AppDbContext db) : ICatalogStore
{
    private const int SqliteConstraintError = 19;

    public IReadOnlyList<Product> List() =>
        db.Products.AsNoTracking().OrderBy(x => x.Sku)
          .AsEnumerable()               // далі — уже в пам'яті: Product.Create у SQL не перекладається
          .Select(ToDomain).ToList();

    public Product? GetById(string id) =>
        db.Products.AsNoTracking().SingleOrDefault(x => x.Id == id) is { } row ? ToDomain(row) : null;

    public void Add(Product item)
    {
        ArgumentNullException.ThrowIfNull(item);

        db.Products.Add(ToRow(item));
        Save(item);
    }

    public void Update(Product item)
    {
        ArgumentNullException.ThrowIfNull(item);

        ProductRow row = db.Products.Single(x => x.Id == item.Id);
        row.Sku = item.Sku;
        row.Name = item.Name;
        row.Unit = item.Unit;
        row.Quantity = item.Quantity;
        Save(item);
    }

    public bool Remove(string id)
    {
        ProductRow? row = db.Products.SingleOrDefault(x => x.Id == id);
        if (row is null)
            return false;

        db.Products.Remove(row);
        db.SaveChanges();
        return true;
    }

    // Рядок таблиці -> доменний об'єкт: тільки через фабрику, тобто з усіма перевірками інваріантів.
    private static Product ToDomain(ProductRow row) =>
        Product.Create(row.Id, row.Sku, row.Name, row.Unit, row.Quantity);

    private static ProductRow ToRow(Product p) =>
        new() { Id = p.Id, Sku = p.Sku, Name = p.Name, Unit = p.Unit, Quantity = p.Quantity };

    private void Save(Product item)
    {
        try
        {
            db.SaveChanges();
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteErrorCode: SqliteConstraintError })
        {
            // Унікальний індекс по Sku: замість «UNIQUE constraint failed» — зрозуміла доменна помилка.
            db.ChangeTracker.Clear();
            throw new DomainException($"Товар з артикулом {item.Sku} уже існує.", ex);
        }
    }
}
