namespace Contracts;

// Сторінка списку: записи цієї сторінки і загальна кількість знайдених.
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
