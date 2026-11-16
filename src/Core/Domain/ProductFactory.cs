using Core.Dto;

namespace Core.Domain;

// Перетворює результат імпорту (DTO) на сутності. Та сама ідея «дані + помилки», що й в імпорті.
public static class ProductFactory
{
    public static ImportResult<Product> FromImport(ImportResult<ProductDto> source)
    {
        var products = new List<Product>();
        var errors = new List<string>(source.Errors);

        foreach (ProductDto dto in source.Items)
        {
            try
            {
                products.Add(Product.FromDto(dto));
            }
            catch (ArgumentException ex)
            {
                errors.Add($"запис {dto.Id}: {ex.Message}");
            }
        }

        return new ImportResult<Product>(products, errors);
    }
}
