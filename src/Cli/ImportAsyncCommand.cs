using System.Globalization;
using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Services;
using Core.Storage;

namespace Cli;

// Асинхронний імпорт зі скасуванням (ЛР8).
internal static class ImportAsyncCommand
{
    public static async Task<int> RunAsync(CatalogService service, string[] args)
    {
        // Шлях — перший аргумент після назви команди, інакше data/import.json.
        string path = args.Skip(1).FirstOrDefault(a => !a.StartsWith("--")) ?? Path.Combine("data", "import.json");

        // Два джерела скасування: користувач (Ctrl+C) і таймаут. Зв'язаний токен спрацьовує від будь-якого.
        using var userCts = new CancellationTokenSource();
        using var timeoutCts = new CancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(userCts.Token, timeoutCts.Token);

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;        // не вбивати процес, а дати програмі завершитися самій
            userCts.Cancel();
        };

        double? timeout = ReadSeconds(args, "--timeout=");
        if (timeout is not null)
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeout.Value));

        // --slow=N імітує великий файл: чекає N секунд перед читанням (щоб було що скасовувати).
        ICatalogImporter importer = new JsonCatalogImporter();
        double? delay = ReadSeconds(args, "--slow=");
        if (delay is not null)
            importer = new SlowImporter(importer, TimeSpan.FromSeconds(delay.Value));

        try
        {
            int added;
            if (args.Contains("--stream"))
            {
                added = await service.ImportStreamAsync(new JsonCatalogImporter().StreamAsync(path, linkedCts.Token), linkedCts.Token);
            }
            else
            {
                IProgress<int>? progress = args.Contains("--progress") ? new ConsoleProgress() : null;
                added = await service.ImportAsync(importer, path, linkedCts.Token, progress);
            }

            Console.WriteLine($"Імпортовано записів: {added}");
            Console.WriteLine($"Усього у сховищі: {service.All().Count}");
            return 0;
        }
        catch (OperationCanceledException)
        {
            // Скасування — не помилка; тому окремий catch і він стоїть ПЕРШИМ.
            string reason = timeoutCts.IsCancellationRequested ? "спрацював таймаут" : "натиснуто Ctrl+C";
            Console.WriteLine($"Імпорт скасовано ({reason}).");
            return 3;
        }
        catch (DomainException ex)
        {
            Console.WriteLine($"Дані відхилено: {ex.Message.ReplaceLineEndings(" ")}");
            if (ex.InnerException is not null)
                Console.WriteLine($"  причина: {ex.InnerException.GetType().Name}");
            return 4;
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
            return 1;
        }
    }

    private static double? ReadSeconds(string[] args, string prefix)
    {
        string? arg = args.FirstOrDefault(a => a.StartsWith(prefix));

        return arg is not null &&
               double.TryParse(arg[prefix.Length..], NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds)
            ? seconds
            : null;
    }

    // Обгортка над імпортером: затримка перед читанням. Токен передається і в затримку, і далі.
    private sealed class SlowImporter(ICatalogImporter inner, TimeSpan delay) : ICatalogImporter
    {
        public async Task<IReadOnlyList<Product>> LoadAsync(string path, CancellationToken ct = default)
        {
            Console.WriteLine($"Читаю файл (імітація великого файлу, {delay.TotalSeconds:0.#} с)... Ctrl+C — скасувати");
            await Task.Delay(delay, ct);
            return await inner.LoadAsync(path, ct);
        }
    }

    // Підписник на хід імпорту (додаткове завдання 3).
    private sealed class ConsoleProgress : IProgress<int>
    {
        private int _last = -1;

        public void Report(int percent)
        {
            if (percent / 25 == _last / 25 && _last >= 0)
                return;                         // друкуємо лише кожні 25 %

            _last = percent;
            Console.WriteLine($"  хід імпорту: {percent,3}%");
        }
    }
}
