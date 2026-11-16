using Core.Domain;
using Core.Dto;
using Core.Import;

namespace Core.Tests;

public class CsvImportTests
{
    private const string Csv = """
        id;sku;name;unit;quantity
        P-1;SKU-1;Цемент;шт;120
        P-2;SKU-2;Пісок;т;18

        P-3;SKU-3;Гіпсокартон;лист
        P-4;SKU-4;Стрічка;м;багато
        P-5;;Без артикулу;шт;10
        P-6;SKU-6;Дріт;;15
        """;

    [Fact]
    public void Load_ReturnsGoodRows_TogetherWithErrorsAndLineNumbers()
    {
        using var file = new TempFile(".csv", Csv);

        ImportResult<ProductDto> result = ProductCsvImporter.Load(file.Path);

        Assert.Equal(3, result.Items.Count);                 // P-1, P-2 і P-6 (порожню одиницю парсер не перевіряє)
        Assert.Equal(3, result.Errors.Count);
        Assert.StartsWith("рядок 5:", result.Errors[0]);     // порожній рядок 4 не є помилкою
        Assert.Equal(50.0, result.ErrorPercent);
    }

    [Fact]
    public void Load_CallsOnSkipped_ForEveryBadRow()
    {
        using var file = new TempFile(".csv", Csv);
        var skipped = new List<string>();

        ProductCsvImporter.Load(file.Path, skipped.Add);

        Assert.Equal(3, skipped.Count);
    }

    [Fact]
    public void ProductFactory_FromImport_RejectsRowsThatBreakInvariants()
    {
        using var file = new TempFile(".csv", Csv);

        ImportResult<Product> result = ProductFactory.FromImport(ProductCsvImporter.Load(file.Path));

        Assert.Equal(new[] { "P-1", "P-2" }, result.Items.Select(p => p.Id));
        Assert.Equal(4, result.Errors.Count);                // 3 від парсера + P-6 без одиниці виміру
        Assert.Contains("P-6", result.Errors[3]);
    }
}
