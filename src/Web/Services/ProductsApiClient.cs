using System.Net;
using Contracts;
using Web.Contracts;

namespace Web.Services;

// Усі HTTP-виклики до Api в одному місці. Адреси Api тут немає: вона в HttpClient.BaseAddress із конфігурації.
public sealed class ProductsApiClient(HttpClient http)
{
    // Список з пошуком і сортуванням на боці Api.
    public async Task<PagedResult<ProductDto>> ListAsync(
        string? search, string sortBy, bool desc, CancellationToken ct = default)
    {
        string url = $"api/v1/products?pageSize=100&sortBy={Uri.EscapeDataString(sortBy)}&desc={desc.ToString().ToLowerInvariant()}";
        if (!string.IsNullOrWhiteSpace(search))
            url += $"&search={Uri.EscapeDataString(search)}";

        return await http.GetFromJsonAsync<PagedResult<ProductDto>>(url, ct)
            ?? new PagedResult<ProductDto>([], 0, 1, 100);
    }

    // 404 — це не виняток, а відповідь «немає такого»: повертаємо null.
    public async Task<ProductModel?> FindAsync(string id, CancellationToken ct = default)
    {
        using HttpResponseMessage response = await http.GetAsync($"api/v1/products/{Uri.EscapeDataString(id)}", ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        ProductDto? dto = await response.Content.ReadFromJsonAsync<ProductDto>(ct);
        return dto is null ? null : ProductModel.From(dto);
    }

    public Task<ApiResult> CreateAsync(ProductModel model, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post, "api/v1/products", model.ToRequest(), ct);

    public Task<ApiResult> UpdateAsync(ProductModel model, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Put, $"api/v1/products/{Uri.EscapeDataString(model.Id)}", model.ToRequest(), ct);

    public Task<ApiResult> DeleteAsync(string id, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Delete, $"api/v1/products/{Uri.EscapeDataString(id)}", null, ct);

    private async Task<ApiResult> SendAsync(HttpMethod method, string url, SaveProductRequest? body, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url);
            if (body is not null)
                request.Content = JsonContent.Create(body);

            using HttpResponseMessage response = await http.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
                return new ApiResult(true);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return new ApiResult(false, "Запис не знайдено — можливо, його вже видалили.");

            ValidationProblemResponse? problem = await ReadProblemAsync(response, ct);

            return response.StatusCode switch
            {
                // 400: помилки по полях показуємо біля полів форми
                HttpStatusCode.BadRequest => new ApiResult(false, "Дані не пройшли перевірку на сервері.", problem?.Errors),
                // 409: порушено бізнес-правило — загальне повідомлення з тексту сервера
                HttpStatusCode.Conflict => new ApiResult(false, problem?.Detail ?? "Операцію відхилено бізнес-правилом."),
                _ => new ApiResult(false, $"Сервер повернув {(int)response.StatusCode}.")
            };
        }
        catch (HttpRequestException)
        {
            // Api не запущений або недоступний по мережі
            return new ApiResult(false, "Api недоступний. Перевірте, чи запущено src/Api.");
        }
    }

    private static async Task<ValidationProblemResponse?> ReadProblemAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<ValidationProblemResponse>(ct);
        }
        catch (System.Text.Json.JsonException)
        {
            return null;        // тіло відповіді не у форматі ProblemDetails
        }
    }
}
