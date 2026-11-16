namespace Core.Reports;

public static class EnumerableExtensions
{
    // Власний метод розширення: n елементів з найбільшим значенням ключа.
    // Слово this перед першим параметром дозволяє писати source.TopBy(...).
    public static IEnumerable<T> TopBy<T>(this IEnumerable<T> source, Func<T, int> keySelector, int n) =>
        source.OrderByDescending(keySelector).Take(n);
}
