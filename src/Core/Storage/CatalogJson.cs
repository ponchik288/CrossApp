using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Domain;

namespace Core.Storage;

// Єдиний формат JSON для каталогу: camelCase, відступи, кирилиця без \uXXXX.
public static class CatalogJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    // Атомарний запис: спершу у тимчасовий файл, потім заміна. Якщо програма впаде посеред запису,
    // старий файл залишиться цілим.
    public static void WriteAtomic(string path, IEnumerable<Product> products)
    {
        string fullPath = Path.GetFullPath(path);
        string tmp = fullPath + ".tmp";

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(tmp, JsonSerializer.Serialize(products.Select(p => p.ToDto()).ToList(), Options));
        File.Move(tmp, fullPath, overwrite: true);
    }
}
