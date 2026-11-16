using Core.Abstractions;
using Core.Domain;

namespace Core.Services;

// Бізнес-операції над каталогом товарів. Сервіс знає лише інтерфейс сховища.
public sealed class CatalogService(ICatalogStore store)
{
    private readonly ICatalogStore _store = store ?? throw new ArgumentNullException(nameof(store));

    public Product Add(string sku, string name, string unit, int quantity)
    {
        // Інваріанти перевіряє Product.Create; сховище перевіряє лише унікальність id.
        Product product = Product.Create(Guid.NewGuid().ToString("N")[..8], sku, name, unit, quantity);
        _store.Add(product);
        return product;
    }

    // Прихід товару.
    public void Receive(string id, int quantity)
    {
        Product product = Require(id);
        product.RegisterArrival(quantity);
        _store.Update(product);
    }

    // Видача товару.
    public void Issue(string id, int quantity)
    {
        Product product = Require(id);
        product.Issue(quantity);
        _store.Update(product);
    }

    public bool Remove(string id) => _store.Remove(id);

    public IReadOnlyList<Product> All() => _store.List();

    public Product? Find(string id) => _store.GetById(id);

    // Пошук за довільною умовою: умову передає той, хто викликає.
    public IReadOnlyList<Product> FindAll(Func<Product, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _store.List().Where(predicate).ToList();
    }

    private Product Require(string id) =>
        _store.GetById(id) ?? throw new InvalidOperationException($"Немає запису з id={id}.");
}
