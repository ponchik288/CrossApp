using Core.Domain;

namespace Core.Tests;

public class ProductTests
{
    [Theory]
    [InlineData("", "SKU-1", "Назва", "шт")]
    [InlineData("P-1", " ", "Назва", "шт")]
    [InlineData("P-1", "SKU-1", "", "шт")]
    [InlineData("P-1", "SKU-1", "Назва", "")]
    public void Create_WithEmptyRequiredField_ThrowsArgumentException(string id, string sku, string name, string unit) =>
        Assert.Throws<ArgumentException>(() => Product.Create(id, sku, name, unit, 1));

    [Fact]
    public void Create_WithNegativeQuantity_ThrowsArgumentOutOfRange() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Product.Create("P-1", "SKU-1", "Назва", "шт", -5));

    [Fact]
    public void Create_NormalizesSkuToUpperCase() =>
        Assert.Equal("SKU-001", Product.Create("P-1", " sku-001 ", "Назва", "шт", 1).Sku);

    [Fact]
    public void Issue_MoreThanStock_ThrowsDomainException_AndKeepsState()
    {
        Product product = Product.Create("P-1", "SKU-1", "Назва", "шт", 120);

        DomainException ex = Assert.Throws<DomainException>(() => product.Issue(1000));

        Assert.Contains("120", ex.Message);
        Assert.Equal(120, product.Quantity);
    }

    [Fact]
    public void ToDto_ThenFromDto_RestoresSameData()
    {
        Product original = Product.Create("P-1", "SKU-1", "Назва", "шт", 7);

        Product restored = Product.FromDto(original.ToDto());

        Assert.Equal(original.ToDto(), restored.ToDto());      // record порівнюється за значенням
    }
}
