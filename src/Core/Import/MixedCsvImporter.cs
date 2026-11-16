using System.Text;
using Core.Dto;

namespace Core.Import;

// Файл із різними рядками: "P;..." – товар, "W;..." – склад.
public static class MixedCsvImporter
{
    private const char Separator = ';';

    public static MixedImportResult Load(string path)
    {
        var products = new List<ProductDto>();
        var warehouses = new List<WarehouseDto>();
        var errors = new List<string>();

        string[] lines = File.ReadAllLines(path, Encoding.UTF8);

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            switch (ParseLine(line))
            {
                case ProductRow row:
                    products.Add(row.Value);
                    break;
                case WarehouseRow row:
                    warehouses.Add(row.Value);
                    break;
                case FailedRow failed:
                    errors.Add($"рядок {i + 1}: {failed.Reason}");
                    break;
            }
        }

        return new MixedImportResult(products, warehouses, errors);
    }

    // Один switch, два різні типи результату: перший елемент масиву вирішує, що це за рядок.
    private static Row ParseLine(string line)
    {
        string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

        return parts switch
        {
            ["P", not "" and var id, not "" and var sku, not "" and var name, var unit, var qty]
                when ProductCsvImporter.TryParseQuantity(qty, out int q)
                => new ProductRow(new ProductDto(id, sku, name, unit, q)),
            ["W", not "" and var id, not "" and var name]
                => new WarehouseRow(new WarehouseDto(id, name)),
            ["W", not "" and var id, not "" and var name, var address]
                => new WarehouseRow(new WarehouseDto(id, name, address)),
            ["P", ..]
                => new FailedRow("товар має бути у форматі P;id;sku;name;unit;quantity"),
            ["W", ..]
                => new FailedRow("склад має бути у форматі W;id;name[;address]"),
            [var prefix, ..]
                => new FailedRow($"невідомий тип рядка '{prefix}'"),
            _ => new FailedRow("порожній рядок")
        };
    }

    private abstract record Row;
    private sealed record ProductRow(ProductDto Value) : Row;
    private sealed record WarehouseRow(WarehouseDto Value) : Row;
    private sealed record FailedRow(string Reason) : Row;
}
