using Core.Domain;

namespace Core.Abstractions;

// Контракт сховища товарів: що воно вміє, без жодного слова про те, ДЕ лежать дані.
public interface ICatalogStore
{
    IReadOnlyList<Product> List();
    Product? GetById(string id);
    void Add(Product item);
    void Update(Product item);
    bool Remove(string id);
}
