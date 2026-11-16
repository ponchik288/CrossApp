using Core.Domain;

namespace Core.Tests;

// Маленький фіксований набір записів для тестів (не SampleData: тести не мають залежати від демо-даних).
internal static class TestData
{
    public static IReadOnlyList<Product> Items() =>
    [
        Product.Create("T-1", "SKU-BIG", "Великий залишок", "шт", 500),
        Product.Create("T-2", "SKU-MID", "Середній залишок", "шт", 50),
        Product.Create("T-3", "SKU-LOW", "Малий залишок", "кг", 5),
        Product.Create("T-4", "SKU-NIL", "Нульовий залишок", "кг", 0),
        Product.Create("T-5", "SKU-TIE", "Такий самий, як середній", "м", 50)
    ];
}
