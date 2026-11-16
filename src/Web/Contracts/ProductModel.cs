using System.ComponentModel.DataAnnotations;
using Contracts;

namespace Web.Contracts;

// Модель форми. Атрибути дають швидку перевірку в браузері; остаточні правила — на сервері.
public sealed class ProductModel
{
    public string Id { get; set; } = "";

    [Required(ErrorMessage = "Артикул обов'язковий.")]
    [StringLength(50, ErrorMessage = "Артикул: до 50 символів.")]
    public string Sku { get; set; } = "";

    [Required(ErrorMessage = "Назва обов'язкова.")]
    [StringLength(200, ErrorMessage = "Назва: до 200 символів.")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Одиниця виміру обов'язкова.")]
    [StringLength(20, ErrorMessage = "Одиниця виміру: до 20 символів.")]
    public string Unit { get; set; } = "";

    [Range(0, int.MaxValue, ErrorMessage = "Кількість не може бути від'ємною.")]
    public int Quantity { get; set; }

    public static ProductModel From(ProductDto dto) =>
        new() { Id = dto.Id, Sku = dto.Sku, Name = dto.Name, Unit = dto.Unit, Quantity = dto.Quantity };

    public SaveProductRequest ToRequest() => new(Sku, Name, Unit, Quantity);
}
