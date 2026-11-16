using System.Net.Http.Json;
using Contracts;

namespace Api.Tests;

public class PersistenceTests
{
    // Доказ персистентності: дані, записані одним екземпляром Api, бачить інший (той самий файл БД).
    [Fact]
    public async Task Data_Survives_ApiRestart()
    {
        using var first = new ApiFactory();
        HttpClient client1 = first.CreateClient();
        await client1.PostAsJsonAsync("/api/products", new SaveProductRequest("T-900", "Переживе перезапуск", "шт", 3));

        // «Перезапуск»: новий хост застосунку, але з тим самим файлом бази.
        using var second = first.WithWebHostBuilder(_ => { });
        HttpClient client2 = second.CreateClient();
        PagedResult<ProductDto>? page = await client2.GetFromJsonAsync<PagedResult<ProductDto>>("/api/products?search=T-900");

        Assert.Equal(1, page!.Total);
        Assert.Equal("Переживе перезапуск", page.Items[0].Name);
    }
}
