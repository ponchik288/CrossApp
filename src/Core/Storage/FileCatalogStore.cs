using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

// Файлове сховище: той самий контракт, але дані лежать у JSON-файлі.
// Файл читається один раз при першому зверненні (кеш), а записується після кожної зміни.
public sealed class FileCatalogStore(string path) : ICatalogStore
{
    private readonly Dictionary<string, Product> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _path = Path.GetFullPath(path);
    private bool _loaded;

    public IReadOnlyList<Product> List()
    {
        EnsureLoaded();
        return _cache.Values.ToList();
    }

    public Product? GetById(string id)
    {
        EnsureLoaded();
        return _cache.GetValueOrDefault(id);
    }

    public void Add(Product item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureLoaded();

        if (_cache.ContainsKey(item.Id))
            throw new InvalidOperationException($"Запис з id={item.Id} уже існує.");

        _cache.Add(item.Id, item);
        Flush();
    }

    public void Update(Product item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureLoaded();
        _cache[item.Id] = item;
        Flush();
    }

    public bool Remove(string id)
    {
        EnsureLoaded();

        if (!_cache.Remove(id))
            return false;

        Flush();
        return true;
    }

    private void EnsureLoaded()
    {
        if (_loaded)
            return;

        if (!File.Exists(_path))
        {
            _loaded = true;
            return;
        }

        List<ProductDto?> dtos;
        try
        {
            dtos = JsonSerializer.Deserialize<List<ProductDto?>>(File.ReadAllText(_path), CatalogJson.Options) ?? [];
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Файл даних {_path} пошкоджений: це не коректний JSON.", ex);
        }

        _cache.Clear();
        for (int i = 0; i < dtos.Count; i++)
        {
            try
            {
                // На диску лежать DTO; FromDto повторно перевіряє інваріанти.
                Product product = Product.FromDto(dtos[i] ?? throw new ArgumentException("порожній елемент"));
                _cache[product.Id] = product;
            }
            catch (ArgumentException ex)
            {
                throw new InvalidDataException($"Файл даних {_path}: запис {i + 1} некоректний ({ex.Message}).", ex);
            }
        }

        _loaded = true;
    }

    private void Flush() => CatalogJson.WriteAtomic(_path, _cache.Values);
}
