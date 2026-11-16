using Core.Domain;

namespace Core.Tests;

public class DomainRulesTests
{
    [Fact]
    public void Warehouse_AddProduct_WithSameSku_ThrowsDomainException()
    {
        Warehouse warehouse = Warehouse.Create("W-1", "Склад");
        warehouse.AddProduct(Product.Create("P-1", "SKU-1", "Цемент", "шт", 1));

        Assert.Throws<DomainException>(() =>
            warehouse.AddProduct(Product.Create("P-2", "sku-1", "Цемент (дубль)", "шт", 1)));

        Assert.Single(warehouse.Products);
    }

    [Fact]
    public void StockTransfer_Move_MovesQuantityBetweenSameSku()
    {
        Product from = Product.Create("P-1", "SKU-1", "Клей", "кг", 100);
        Product to = Product.Create("P-2", "SKU-1", "Клей", "кг", 0);

        StockTransfer.Move(from, to, 40);

        Assert.Equal(60, from.Quantity);
        Assert.Equal(40, to.Quantity);
    }

    [Fact]
    public void StockTransfer_Move_BetweenDifferentSku_Throws_AndChangesNothing()
    {
        Product from = Product.Create("P-1", "SKU-1", "Клей", "кг", 100);
        Product other = Product.Create("P-2", "SKU-2", "Гіпс", "кг", 5);

        Assert.Throws<DomainException>(() => StockTransfer.Move(from, other, 10));

        Assert.Equal(100, from.Quantity);
        Assert.Equal(5, other.Quantity);
    }

    [Fact]
    public void Movement_Draft_CanBeConfirmed()
    {
        Movement movement = Movement.Create("M-1", "SKU-1", 10);

        movement.Confirm();

        Assert.Equal(MovementStatus.Confirmed, movement.Status);
    }

    [Fact]
    public void Movement_Confirmed_CannotBeCancelledOrConfirmedAgain()
    {
        Movement movement = Movement.Create("M-1", "SKU-1", 10);
        movement.Confirm();

        Assert.Throws<DomainException>(movement.Cancel);
        Assert.Throws<DomainException>(movement.Confirm);
        Assert.Equal(MovementStatus.Confirmed, movement.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Movement_Create_WithNonPositiveAmount_Throws(int amount) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Movement.Create("M-1", "SKU-1", amount));
}
