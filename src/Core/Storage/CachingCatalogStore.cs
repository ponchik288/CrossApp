using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

// Декоратор: приймає ІНШЕ сховище і додає до нього кешування списку.
// Контракт той самий (ICatalogStore), тому сервіс різниці не помічає.
public sealed class CachingCatalogStore(ICatalogStore inner) : ICatalogStore
{
    private readonly ICatalogStore _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private IReadOnlyList<Product>? _list;

    public int Hits { get; private set; }
    public int Misses { get; private set; }

    public IReadOnlyList<Product> List()
    {
        if (_list is null)
        {
            Misses++;
            _list = _inner.List();
        }
        else
        {
            Hits++;
        }

        return _list;
    }

    public Product? GetById(string id) => _inner.GetById(id);

    public void Add(Product item)
    {
        _inner.Add(item);
        _list = null;       // дані змінилися, кеш списку більше не дійсний
    }

    public void Update(Product item)
    {
        _inner.Update(item);
        _list = null;
    }

    public bool Remove(string id)
    {
        bool removed = _inner.Remove(id);
        if (removed)
            _list = null;

        return removed;
    }

    public override string ToString() => $"CachingCatalogStore({_inner.GetType().Name})";
}
