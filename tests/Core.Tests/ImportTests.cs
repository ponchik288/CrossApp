using Core.Abstractions;
using Core.Domain;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

public class ImportTests
{
    // 5) скасування: токен уже скасований, файлу навіть не існує
    [Fact]
    public async Task ImportAsync_WhenAlreadyCancelled_Throws()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var service = new CatalogService(new InMemoryCatalogStore());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(new JsonCatalogImporter(), "any.json", cts.Token));
    }

    // 6) зіпсований запис у тимчасовому файлі -> DomainException з номером запису
    [Fact]
    public async Task ImportAsync_WithInvalidRecord_ThrowsDomainException_WithRecordNumber()
    {
        string path = Path.Combine(Path.GetTempPath(), $"crossapp-test-{Guid.NewGuid():N}.json");
        try
        {
            const string json = """
                [
                  { "id": "A", "sku": "S-1", "name": "Добрий", "unit": "шт", "quantity": 1 },
                  { "id": "B", "sku": "", "name": "Без артикулу", "unit": "шт", "quantity": 1 }
                ]
                """;
            await File.WriteAllTextAsync(path, json);
            var service = new CatalogService(new InMemoryCatalogStore());

            DomainException ex = await Assert.ThrowsAsync<DomainException>(
                () => service.ImportAsync(new JsonCatalogImporter(), path));

            Assert.StartsWith("Запис 2:", ex.Message);
            Assert.IsType<ArgumentException>(ex.InnerException);
            Assert.Empty(service.All());           // нічого не додано частково
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ImportAsync_Twice_AddsRecordsOnlyOnce()
    {
        var service = new CatalogService(new InMemoryCatalogStore());
        var importer = new FakeImporter(TestData.Items());

        int first = await service.ImportAsync(importer, "не використовується");
        int second = await service.ImportAsync(importer, "не використовується");

        Assert.Equal(5, first);
        Assert.Equal(0, second);
        Assert.Equal(5, service.All().Count);
    }

    // Зв'язані токени: доводимо, що спрацював саме таймаут, а не «користувач».
    [Fact]
    public async Task ImportAsync_WithLinkedTokens_IsCancelledByTimeout_NotByUser()
    {
        using var userCts = new CancellationTokenSource();
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(userCts.Token, timeoutCts.Token);
        var service = new CatalogService(new InMemoryCatalogStore());
        var slowImporter = new FakeImporter(TestData.Items(), delay: TimeSpan.FromSeconds(30));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(slowImporter, "не використовується", linked.Token));

        Assert.True(timeoutCts.IsCancellationRequested);
        Assert.False(userCts.IsCancellationRequested);
        Assert.Empty(service.All());
    }

    [Fact]
    public async Task ImportAsync_ReportsProgressUpTo100()
    {
        var service = new CatalogService(new InMemoryCatalogStore());
        var progress = new ListProgress();

        await service.ImportAsync(new FakeImporter(TestData.Items()), "не використовується", progress: progress);

        Assert.Equal(new[] { 20, 40, 60, 80, 100 }, progress.Values);
    }

    // Імпортер-заглушка: віддає готові записи без диска; затримка дозволяє перевірити скасування.
    private sealed class FakeImporter(IReadOnlyList<Product> items, TimeSpan? delay = null) : ICatalogImporter
    {
        public async Task<IReadOnlyList<Product>> LoadAsync(string path, CancellationToken ct = default)
        {
            if (delay is not null)
                await Task.Delay(delay.Value, ct);

            return items;
        }
    }

    // Синхронний IProgress: записує значення одразу, без перемикання потоків.
    private sealed class ListProgress : IProgress<int>
    {
        public List<int> Values { get; } = [];

        public void Report(int value) => Values.Add(value);
    }
}
