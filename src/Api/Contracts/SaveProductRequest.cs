namespace Api.Contracts;

// Вхідний DTO для POST і PUT. Id тут немає: його задає сервер або маршрут.
public sealed record SaveProductRequest(string Sku, string Name, string Unit, int Quantity);
