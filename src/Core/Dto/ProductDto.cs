namespace Core.Dto;

// Один рядок файлу з товарами. Note необов'язкова, тому string? і значення за замовчуванням.
public record ProductDto(
    string Id,
    string Sku,
    string Name,
    string Unit,
    int Quantity,
    string? Note = null);
