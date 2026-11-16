using System.Text;
using System.Text.Json;
using Core.Dto;

namespace Core.Import;

// Другий імпортер на тих самих типах: читає масив товарів із JSON-файлу.
public static class ProductJsonImporter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static ImportResult<ProductDto> Load(string path)
    {
        string json = File.ReadAllText(path, Encoding.UTF8);

        List<ProductDto?> rows;
        try
        {
            rows = JsonSerializer.Deserialize<List<ProductDto?>>(json, Options) ?? [];
        }
        catch (JsonException ex)
        {
            return new ImportResult<ProductDto>([], [$"файл не є коректним JSON: {ex.Message}"]);
        }

        var items = new List<ProductDto>();
        var errors = new List<string>();

        for (int i = 0; i < rows.Count; i++)
        {
            ProductDto? row = rows[i];

            // У JSON поле може бути відсутнім, тоді в record потрапляє null.
            string? problem = row switch
            {
                null => "порожній елемент",
                { Id: null or "" } => "ідентифікатор порожній",
                { Sku: null or "" } or { Name: null or "" } => "SKU або назва порожні",
                { Unit: null or "" } => "одиниця виміру порожня",
                { Quantity: < 0 } => $"кількість {row.Quantity} від'ємна",
                _ => null
            };

            if (problem is null)
                items.Add(row!);
            else
                errors.Add($"елемент {i + 1}: {problem}");
        }

        return new ImportResult<ProductDto>(items, errors);
    }
}
