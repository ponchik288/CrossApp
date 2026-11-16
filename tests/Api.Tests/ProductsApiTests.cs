using System.Net;
using System.Net.Http.Json;
using Api.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Api.Tests;

// Інтеграційні тести: Api піднімається в пам'яті (без порту і мережі), запити йдуть через HttpClient.
public class ProductsApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
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
}
