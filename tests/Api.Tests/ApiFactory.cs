using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace Api.Tests;

// Api для тестів: працює з власним тимчасовим файлом SQLite, а не з data/app.db розробника.
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public string DatabasePath { get; } =
        Path.Combine(Path.GetTempPath(), $"crossapp-api-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");                                  // щоб міграції застосувалися під час запуску
        builder.UseSetting("ConnectionStrings:AppDb", $"Data Source={DatabasePath}");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();           // звільнити файл перед видаленням
        foreach (string suffix in new[] { "", "-shm", "-wal" })
            File.Delete(DatabasePath + suffix);
    }
}
