using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

// Сховище в пам'яті: словник за Id. Після завершення програми дані зникають.
// Усі операції під lock: в Api сховище одне на всі запити, а запити обробляються одночасно.
public sealed class InMemoryCatalogStore(IEnumerable<Product>? seed = null) : ICatalogStore
{
    private readonly Dictionary<string, Product> _items =
        (seed ?? []).ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);

    private readonly object _gate = new();

    public IReadOnlyList<Product> List()
    {
        lock (_gate)
            return _items.Values.ToList();
    }

    public Product? GetById(string id)
    {
        lock (_gate)
            return _items.GetValueOrDefault(id);
    }

    public void Add(Product item)
    {
        ArgumentNullException.ThrowIfNull(item);

        lock (_gate)
        {
            if (_items.ContainsKey(item.Id))
                throw new InvalidOperationException($"Запис з id={item.Id} уже існує.");

            _items.Add(item.Id, item);
        }
    }

    public void Update(Product item)
    {
        ArgumentNullException.ThrowIfNull(item);

        lock (_gate)
            _items[item.Id] = item;
    }

    public bool Remove(string id)
    {
        lock (_gate)
            return _items.Remove(id);
    }
}
