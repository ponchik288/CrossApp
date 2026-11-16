namespace Core.Reports;

// Зведення «товар × переміщення»: скільки документів і на яку кількість.
public sealed record MovementSummary(string Sku, string Name, int Movements, int TotalAmount);
