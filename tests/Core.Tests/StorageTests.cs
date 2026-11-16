using Core.Domain;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

public class StorageTests
{
    [Fact]
    public void InMemoryStore_Add_WithDuplicateId_Throws()
    {
        var store = new InMemoryCatalogStore();
        store.Add(Product.Create("P-1", "SKU-1", "Цемент", "шт", 1));

        Assert.Throws<InvalidOperationException>(() =>
            store.Add(Product.Create("p-1", "SKU-9", "Інший", "шт", 1)));     // id без урахування регістру
    }

    // Єдиний тест сховища, який пише на диск: у тимчасовий файл, що видаляється після тесту.
    [Fact]
    public void FileStore_KeepsData_BetweenTwoInstances()
    {
        using var file = new TempFile(".json");

        var first = new CatalogService(new FileCatalogStore(file.Path));
        Product created = first.Add("SKU-1", "Кабель", "м", 50);
        first.Receive(created.Id, 25);

        var second = new CatalogService(new FileCatalogStore(file.Path));

        Assert.Equal(75, second.Find(created.Id)!.Quantity);
        Assert.True(second.Remove(created.Id));
        Assert.Empty(new FileCatalogStore(file.Path).List());
    }

    [Fact]
    public void CachingStore_ReadsInnerListOnce_UntilDataChanges()
    {
        var cache = new CachingCatalogStore(new InMemoryCatalogStore(TestData.Items()));

        cache.List();
        cache.List();
        cache.Add(Product.Create("T-9", "SKU-NEW", "Новий", "шт", 1));
        int count = cache.List().Count;

        Assert.Equal(6, count);
        Assert.Equal(1, cache.Hits);
        Assert.Equal(2, cache.Misses);
    }

    [Fact]
    public async Task StreamImport_AddsAllRecords()
    {
        const string json = """
            [
              { "id": "A", "sku": "S-1", "name": "Перший", "unit": "шт", "quantity": 1 },
              { "id": "B", "sku": "S-2", "name": "Другий", "unit": "шт", "quantity": 2 }
            ]
            """;
        using var file = new TempFile(".json", json);
        var service = new CatalogService(new InMemoryCatalogStore());

        int added = await service.ImportStreamAsync(JsonCatalogImporter.StreamAsync(file.Path));

        Assert.Equal(2, added);
    }
}
