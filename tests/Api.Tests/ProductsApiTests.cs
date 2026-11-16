using System.Net;
using System.Net.Http.Json;
using Contracts;

namespace Api.Tests;

// Інтеграційні тести: Api піднімається в пам'яті (без порту і мережі), запити йдуть через HttpClient.
// База — тимчасовий файл SQLite (див. ApiFactory), який видаляється після тестів.
public class ProductsApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Post_ReturnsCreated_AndGetByLocation_ReturnsTheSameProduct()
    {
        var request = new SaveProductRequest("T-100", "Тестовий товар", "шт", 40);

        HttpResponseMessage created = await _client.PostAsJsonAsync("/api/products", request);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotNull(created.Headers.Location);

        HttpResponseMessage fetched = await _client.GetAsync(created.Headers.Location);

        Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
        ProductDto? product = await fetched.Content.ReadFromJsonAsync<ProductDto>();
        Assert.Equal("T-100", product!.Sku);
        Assert.Equal(40, product.Quantity);
    }

    [Fact]
    public async Task Post_WithInvalidBody_ReturnsBadRequest_WithFieldErrors()
    {
        var request = new SaveProductRequest("", "X", "", -1);

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/products", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        string body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"Sku\"", body);
        Assert.Contains("\"Quantity\"", body);
        Assert.DoesNotContain("Exception", body);
    }

    [Fact]
    public async Task Get_UnknownId_ReturnsNotFound()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/products/no-such-id");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        string body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":404", body);
        Assert.Contains("no-such-id", body);
    }

    [Fact]
    public async Task Issue_MoreThanStock_ReturnsConflict()
    {
        HttpResponseMessage created = await _client.PostAsJsonAsync(
            "/api/products", new SaveProductRequest("T-200", "Малий залишок", "шт", 5));
        ProductDto? product = await created.Content.ReadFromJsonAsync<ProductDto>();

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"/api/products/{product!.Id}/issue", new ChangeQuantityRequest(1000));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ThenGet_ReturnsNotFound()
    {
        HttpResponseMessage created = await _client.PostAsJsonAsync(
            "/api/products", new SaveProductRequest("T-300", "На видалення", "шт", 1));
        Uri location = created.Headers.Location!;

        HttpResponseMessage deleted = await _client.DeleteAsync(location);
        HttpResponseMessage fetched = await _client.GetAsync(location);

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, fetched.StatusCode);
    }

    [Fact]
    public async Task Post_WithExistingSku_ReturnsConflict()
    {
        await _client.PostAsJsonAsync("/api/products", new SaveProductRequest("T-400", "Перший", "шт", 1));

        HttpResponseMessage second = await _client.PostAsJsonAsync(
            "/api/products", new SaveProductRequest("t-400", "Другий з тим самим артикулом", "шт", 1));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Receive_AddsBatchToJournal()
    {
        HttpResponseMessage created = await _client.PostAsJsonAsync(
            "/api/products", new SaveProductRequest("T-500", "З партіями", "шт", 10));
        ProductDto? product = await created.Content.ReadFromJsonAsync<ProductDto>();

        await _client.PostAsJsonAsync($"/api/products/{product!.Id}/receive", new ChangeQuantityRequest(5));
        await _client.PostAsJsonAsync($"/api/products/{product.Id}/receive", new ChangeQuantityRequest(7));

        ProductDto? after = await _client.GetFromJsonAsync<ProductDto>($"/api/products/{product.Id}");
        string batches = await _client.GetStringAsync($"/api/products/{product.Id}/batches");

        Assert.Equal(22, after!.Quantity);
        Assert.Contains("\"amount\":5", batches);
        Assert.Contains("\"amount\":7", batches);
    }

    [Fact]
    public async Task ValidationError_And_NotFound_UseTheSameProblemFormat()
    {
        HttpResponseMessage bad = await _client.PostAsJsonAsync("/api/v1/products", new SaveProductRequest("", "", "", -1));
        HttpResponseMessage missing = await _client.GetAsync("/api/v1/products/no-such-id");

        Assert.Equal("application/problem+json", bad.Content.Headers.ContentType?.MediaType);
        Assert.Equal("application/problem+json", missing.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task VersionedRoute_ReturnsTheSameData_AsUnversioned()
    {
        HttpResponseMessage created = await _client.PostAsJsonAsync(
            "/api/v1/products", new SaveProductRequest("T-600", "Версіонований", "шт", 2));

        Assert.StartsWith("/api/v1/products/", created.Headers.Location!.OriginalString);

        ProductDto? product = await created.Content.ReadFromJsonAsync<ProductDto>();
        ProductDto? viaOldRoute = await _client.GetFromJsonAsync<ProductDto>($"/api/products/{product!.Id}");

        Assert.Equal(product, viaOldRoute);
    }
}
