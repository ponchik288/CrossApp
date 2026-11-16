using System.Globalization;
using System.Text;
using Core.Dto;

namespace Core.Import;

public static class ProductCsvImporter
{
    // Роздільник — крапка з комою: не конфліктує з комою в назвах товарів.
    private const char Separator = ';';

    // onSkipped (необов'язковий) викликається одразу для кожного пропущеного рядка.
    public static ImportResult<ProductDto> Load(string path, Action<string>? onSkipped = null)
    {
        var items = new List<ProductDto>();
        var errors = new List<string>();

        string[] lines = File.ReadAllLines(path, Encoding.UTF8);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            // порожні рядки і коментарі пропускаємо, це не помилки
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            // рядок заголовків
            if (number == 1 && line.StartsWith("id", StringComparison.OrdinalIgnoreCase))
                continue;

            switch (ParseLine(line))
            {
                case ParseOk ok:
                    items.Add(ok.Value);
                    break;
                case ParseFailed failed:
                    string error = $"рядок {number}: {failed.Reason}";
                    errors.Add(error);
                    onSkipped?.Invoke(error);
                    break;
            }
        }

        return new ImportResult<ProductDto>(items, errors);
    }

    private static ParseOutcome ParseLine(string line)
    {
        string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

        return parts switch
        {
            { Length: < 5 }
                => new ParseFailed($"очікую 5 колонок, отримав {parts.Length}"),
            ["", _, _, _, _]
                => new ParseFailed("ідентифікатор порожній"),
            [_, "", _, _, _] or [_, _, "", _, _]
                => new ParseFailed("SKU або назва порожні"),
            [_, _, _, _, var qty] when !TryParseQuantity(qty, out _)
                => new ParseFailed($"кількість '{qty}' не є невід'ємним числом"),
            [var id, var sku, var name, var unit, var qty]
                => new ParseOk(new ProductDto(id, sku, name, unit, ParseQuantity(qty))),
            _ => new ParseFailed($"занадто багато колонок: {parts.Length}")
        };
    }

    // Число розбираємо з InvariantCulture, щоб результат не залежав від мови системи.
    // NumberStyles.None дозволяє лише цифри, тому від'ємне число теж не пройде.
    internal static bool TryParseQuantity(string text, out int quantity) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out quantity);

    private static int ParseQuantity(string text) =>
        int.Parse(text, NumberStyles.None, CultureInfo.InvariantCulture);

    private abstract record ParseOutcome;
    private sealed record ParseOk(ProductDto Value) : ParseOutcome;
    private sealed record ParseFailed(string Reason) : ParseOutcome;
}
