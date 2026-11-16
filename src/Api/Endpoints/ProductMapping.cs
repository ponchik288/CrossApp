using Contracts;
using Core.Domain;

namespace Api.Endpoints;

// Перетворення доменної сутності на DTO контракту. Живе в Api: проєкт Contracts про Core не знає.
internal static class ProductMapping
{
    public static ProductDto ToContract(this Product product) =>
        new(product.Id, product.Sku, product.Name, product.Unit, product.Quantity);
}
