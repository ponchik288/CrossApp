namespace Core.Dto;

// Результат імпорту змішаного файлу: товари і склади в одному файлі.
public sealed record MixedImportResult(
    IReadOnlyList<ProductDto> Products,
    IReadOnlyList<WarehouseDto> Warehouses,
    IReadOnlyList<string> Errors);
