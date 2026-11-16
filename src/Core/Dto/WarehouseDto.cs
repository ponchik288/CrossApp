namespace Core.Dto;

// Склад. Адреса може бути невідома, тому string?.
public record WarehouseDto(
    string Id,
    string Name,
    string? Address = null);
