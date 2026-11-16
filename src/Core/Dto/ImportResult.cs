namespace Core.Dto;

// Результат імпорту: прочитані записи РАЗОМ із переліком помилок.
public sealed record ImportResult<T>(IReadOnlyList<T> Items, IReadOnlyList<string> Errors)
{
    // Статистика імпорту (додаткове завдання 3).
    public int Total => Items.Count + Errors.Count;

    public double ErrorPercent => Total == 0 ? 0 : 100.0 * Errors.Count / Total;
}
