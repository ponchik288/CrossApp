using Core.Abstractions;
using Core.Domain;
using Core.Events;

namespace Core.Services;

// Бізнес-операції над каталогом товарів. Сервіс знає лише інтерфейс сховища.
public sealed class CatalogService(ICatalogStore store, INotificationSink? sink = null)
{
    private readonly ICatalogStore _store = store ?? throw new ArgumentNullException(nameof(store));

    // Подія: сервіс лише повідомляє, що сталося. Хто і як це обробить, він не знає.
    public event EventHandler<CatalogChangedEventArgs>? Changed;

    public Product Add(string sku, string name, string unit, int quantity)
    {
        // Інваріанти перевіряє Product.Create; сховище перевіряє лише унікальність id.
        Product product = Product.Create(Guid.NewGuid().ToString("N")[..8], sku, name, unit, quantity);
        _store.Add(product);
        OnChanged(product, CatalogChangeKind.Added, quantity);
        return product;
    }

    // Прихід товару.
    public void Receive(string id, int quantity)
    {
        Product product = Require(id);
        product.RegisterArrival(quantity);      // якщо тут виняток, події не буде
        _store.Update(product);
        OnChanged(product, CatalogChangeKind.Received, quantity);
    }

    // Видача товару.
    public void Issue(string id, int quantity)
    {
        Product product = Require(id);
        product.Issue(quantity);
        _store.Update(product);
        OnChanged(product, CatalogChangeKind.Issued, -quantity);
    }

    public bool Remove(string id)
    {
        Product? product = _store.GetById(id);
        if (product is null || !_store.Remove(id))
            return false;

        Changed?.Invoke(this, Notified(new CatalogChangedEventArgs(product.Id, CatalogChangeKind.Removed, -product.Quantity, 0)));
        return true;
    }

    public IReadOnlyList<Product> All() => _store.List();

    public Product? Find(string id) => _store.GetById(id);

    // Пошук за довільною умовою: умову передає той, хто викликає.
    public IReadOnlyList<Product> FindAll(Func<Product, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _store.List().Where(predicate).ToList();
    }

    // Відкладений запит: без ToList(), тому виконується лише під час перебирання результату.
    public IEnumerable<Product> Filter(IEnumerable<Product> source, Func<Product, bool> predicate) =>
        source.Where(predicate);

    // Єдине місце, де піднімається подія. Викликається лише ПІСЛЯ успішної зміни.
    private void OnChanged(Product p, CatalogChangeKind kind, int delta) =>
        Changed?.Invoke(this, Notified(new CatalogChangedEventArgs(p.Id, kind, delta, p.Quantity)));

    // Якщо сервісу передали одержувача через конструктор, сповіщаємо і його.
    private CatalogChangedEventArgs Notified(CatalogChangedEventArgs change)
    {
        sink?.Notify(change);
        return change;
    }

    private Product Require(string id) =>
        _store.GetById(id) ?? throw new InvalidOperationException($"Немає запису з id={id}.");
}
