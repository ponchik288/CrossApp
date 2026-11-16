using System.Runtime.CompilerServices;
using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

// Асинхронне читання товарів із JSON-файлу. Справжній асинхронний ввід-вивід, без Task.Run.
public sealed class JsonCatalogImporter : ICatalogImporter
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyList<Product>> LoadAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await using FileStream stream = Open(path);

        List<ProductDto?> dtos;
        try
        {
            dtos = await JsonSerializer.DeserializeAsync<List<ProductDto?>>(stream, Options, ct)
                ?? throw new DomainException($"Файл {Path.GetFileName(path)} не містить списку.");
        }
        catch (JsonException ex)
        {
            // Початковий виняток зберігаємо в InnerException: там рядок і позиція помилки у файлі.
            throw new DomainException($"Файл {Path.GetFileName(path)} не є коректним JSON.", ex);
        }

        var result = new List<Product>(dtos.Count);
        for (int i = 0; i < dtos.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            result.Add(ToProduct(dtos[i], i + 1));
        }

        return result;
    }

    // Потоковий варіант (додаткове завдання 1): записи віддаються по одному, без списку в пам'яті.
    public static async IAsyncEnumerable<Product> StreamAsync(
        string path, [EnumeratorCancellation] CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await using FileStream stream = Open(path);

        int number = 0;
        await foreach (ProductDto? dto in JsonSerializer.DeserializeAsyncEnumerable<ProductDto?>(stream, Options, ct))
        {
            number++;
            yield return ToProduct(dto, number);
        }
    }

    private static FileStream Open(string path) =>
        new(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);

    private static Product ToProduct(ProductDto? dto, int number)
    {
        if (dto is null)
            throw new DomainException($"Запис {number}: порожній елемент.");

        try
        {
            return Product.FromDto(dto);
        }
        catch (ArgumentException ex)
        {
            throw new DomainException($"Запис {number}: {ex.Message}", ex);
        }
    }
}
