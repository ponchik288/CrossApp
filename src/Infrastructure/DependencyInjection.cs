using Core.Abstractions;
using Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    // Реєструє DbContext і сховища. contentRoot потрібен, щоб відносний шлях до файлу БД не залежав від робочого каталогу.
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, string? connectionString, string contentRoot)
    {
        var csb = new SqliteConnectionStringBuilder(connectionString ?? "Data Source=data/app.db");

        if (!Path.IsPathRooted(csb.DataSource))
        {
            string full = Path.GetFullPath(Path.Combine(contentRoot, csb.DataSource));
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            csb.DataSource = full;
        }

        services.AddDbContext<AppDbContext>(options => options.UseSqlite(csb.ConnectionString));   // Scoped
        services.AddScoped<ICatalogStore, EfCatalogStore>();
        services.AddScoped<IBatchJournal, EfBatchJournal>();

        return services;
    }

    // Застосовує міграції, яких ще немає в базі. Викликається під час запуску в Development.
    public static void ApplyMigrations(this IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
    }
}
