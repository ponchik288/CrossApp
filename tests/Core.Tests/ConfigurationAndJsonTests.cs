using Core.Configuration;
using Core.Domain;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

public class ConfigurationAndJsonTests
{
    [Fact]
    public void ResolveFullPath_WithRelativeDataPath_IsBuiltFromBaseDirectory()
    {
        string baseDir = Path.Combine(Path.GetTempPath(), $"crossapp-test-{Guid.NewGuid():N}");
        try
        {
            var storage = new StorageOptions { DataPath = "data", FileName = "items.json" };

            string full = storage.ResolveFullPath(baseDir);

            Assert.Equal(Path.Combine(baseDir, "data", "items.json"), full);
            Assert.True(Directory.Exists(Path.Combine(baseDir, "data")));       // каталог створено кодом
        }
        finally
        {
            Directory.Delete(baseDir, recursive: true);
        }
    }

    [Fact]
    public void ResolveFullPath_WithRootedDataPath_IgnoresBaseDirectory()
    {
        string root = Path.Combine(Path.GetTempPath(), $"crossapp-test-{Guid.NewGuid():N}");
        try
        {
            var storage = new StorageOptions { DataPath = root, FileName = "x.json" };

            Assert.Equal(Path.Combine(root, "x.json"), storage.ResolveFullPath("зовсім-інший-каталог"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void FileStore_WritesCamelCaseJson_WithReadableCyrillic_AndNoTmpFile()
    {
        using var file = new TempFile(".json");
        var service = new CatalogService(new FileCatalogStore(file.Path));

        service.Add("SKU-1", "Цемент М400", "мішок", 40);

        string json = File.ReadAllText(file.Path);
        Assert.Contains("\"name\": \"Цемент М400\"", json);
        Assert.Contains("\"quantity\": 40", json);
        Assert.DoesNotContain("\\u04", json);
        Assert.False(File.Exists(file.Path + ".tmp"));
    }

    [Fact]
    public void FileStore_WithCorruptedJson_ThrowsInvalidDataException_WithInner()
    {
        using var file = new TempFile(".json", "[ { \"id\": ");

        var ex = Assert.Throws<InvalidDataException>(() => new FileCatalogStore(file.Path).List());

        Assert.NotNull(ex.InnerException);
    }

    [Fact]
    public void Export_ThenFileStore_ReadsTheSameCollection()
    {
        using var file = new TempFile(".json");

        CatalogJson.WriteAtomic(file.Path, TestData.Items());

        IReadOnlyList<Product> loaded = new FileCatalogStore(file.Path).List();
        Assert.Equal(TestData.Items().Select(p => p.ToDto()), loaded.Select(p => p.ToDto()));
    }
}
