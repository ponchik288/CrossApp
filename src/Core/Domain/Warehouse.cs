using Core.Dto;

namespace Core.Domain;

// Склад: містить товари. Список товарів назовні віддається лише для читання.
public sealed class Warehouse
{
    private readonly List<Product> _products = [];

    public string Id { get; }
    public string Name { get; }
    public string? Address { get; }

    public IReadOnlyList<Product> Products => _products.AsReadOnly();

    private Warehouse(string id, string name, string? address)
    {
        Id = id;
        Name = name;
        Address = address;
    }

    public static Warehouse Create(string id, string name, string? address = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор складу обов'язковий", nameof(id));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Назва складу не може бути порожньою", nameof(name));

        return new Warehouse(id.Trim(), name.Trim(), string.IsNullOrWhiteSpace(address) ? null : address.Trim());
    }

    // На одному складі не може бути двох товарів з однаковим SKU.
    public void AddProduct(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (FindBySku(product.Sku) is not null)
            throw new InvalidOperationException(
                $"На складі {Id} вже є товар з артикулом {product.Sku}");

        _products.Add(product);
    }

    public Product? FindBySku(string sku) =>
        _products.Find(p => p.Sku == sku.Trim().ToUpperInvariant());

    public WarehouseDto ToDto() => new(Id, Name, Address);

    public static Warehouse FromDto(WarehouseDto dto) => Create(dto.Id, dto.Name, dto.Address);

    public override string ToString() => $"{Id} {Name} — товарів: {_products.Count}";
}
