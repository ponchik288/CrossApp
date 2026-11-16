using Core.Domain;
using Core.Events;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

public class CatalogServiceTests
{
    // 1) інваріант: відмова і незмінний стан
    [Fact]
    public void Receive_WithNegativeQuantity_Throws_AndKeepsState()
    {
        var store = new InMemoryCatalogStore();
        var service = new CatalogService(store);
        Product p = service.Add("SKU-1", "Кабель", "м", 10);

        Assert.ThrowsAny<ArgumentException>(() => service.Receive(p.Id, -5));

        Assert.Equal(10, store.GetById(p.Id)!.Quantity);
    }

    // 2) подія як спосіб перевірки: консоль і диск не потрібні
    [Fact]
    public void Add_RaisesChangedExactlyOnce()
    {
        var service = new CatalogService(new InMemoryCatalogStore());
        var events = new List<CatalogChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);

        service.Add("SKU-2", "Гвинт", "шт", 7);

        Assert.Single(events);
        Assert.Equal(CatalogChangeKind.Added, events[0].Kind);
        Assert.Equal(7, events[0].QuantityAfter);
    }

    [Fact]
    public void Issue_MoreThanStock_ThrowsDomainException_AndRaisesNoEvent()
    {
        var service = new CatalogService(new InMemoryCatalogStore());
        Product p = service.Add("SKU-3", "Цвях", "кг", 5);
        int events = 0;
        service.Changed += (_, _) => events++;

        Assert.Throws<DomainException>(() => service.Issue(p.Id, 100));

        Assert.Equal(0, events);
    }

    [Fact]
    public void Receive_ForMissingId_ThrowsDomainException()
    {
        var service = new CatalogService(new InMemoryCatalogStore());

        Assert.Throws<DomainException>(() => service.Receive("немає", 1));
    }

    [Fact]
    public void ReceiveAndIssue_ChangeQuantityAndReportDelta()
    {
        var service = new CatalogService(new InMemoryCatalogStore());
        Product p = service.Add("SKU-4", "Дріт", "м", 50);
        var deltas = new List<int>();
        service.Changed += (_, e) => deltas.Add(e.Delta);

        service.Receive(p.Id, 25);
        service.Issue(p.Id, 10);

        Assert.Equal(65, service.Find(p.Id)!.Quantity);
        Assert.Equal(new[] { 25, -10 }, deltas);
    }
}
