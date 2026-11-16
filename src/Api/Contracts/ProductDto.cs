using Core.Domain;

namespace Api.Contracts;

// Вихідний DTO: саме так товар виглядає в JSON-відповіді. Доменну сутність клієнту не віддаємо.
public sealed record ProductDto(string Id, string Sku, string Name, string Unit, int Quantity)
{
    public static ProductDto From(Product product) =>
        new(product.Id, product.Sku, product.Name, product.Unit, product.Quantity);
}
