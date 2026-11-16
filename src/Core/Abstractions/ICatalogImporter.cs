using Core.Domain;

namespace Core.Abstractions;

// Асинхронний контракт імпорту товарів.
public interface ICatalogImporter
{
    Task<IReadOnlyList<Product>> LoadAsync(string path, CancellationToken ct = default);
}
