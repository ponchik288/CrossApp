namespace Core.Reports;

// Підсумок по групі: ключ, кількість позицій, сумарний залишок.
public sealed record GroupSummary(string Key, int Count, int TotalQuantity);
