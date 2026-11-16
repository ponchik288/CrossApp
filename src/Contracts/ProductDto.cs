namespace Contracts;

// Вихідний DTO: саме так товар виглядає в JSON-відповіді Api. Використовують і Api, і Web.
public sealed record ProductDto(string Id, string Sku, string Name, string Unit, int Quantity);
