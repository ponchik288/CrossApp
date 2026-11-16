using Contracts;
using FluentValidation;

namespace Api.Validation;

// Правила для вхідного DTO. Ключ помилки — назва поля, тому клієнт може показати її біля поля форми.
public sealed class SaveProductRequestValidator : AbstractValidator<SaveProductRequest>
{
    public SaveProductRequestValidator()
    {
        RuleFor(r => r.Sku)
            .NotEmpty().WithMessage("Артикул обов'язковий.")
            .MaximumLength(50).WithMessage("Артикул: до 50 символів.")
            // Це правило є лише на сервері: клієнтська перевірка зручна, але остаточне слово — за Api.
            .Matches(@"^[\p{L}\p{Nd}-]*$").WithMessage("Артикул: лише літери, цифри і дефіс.");

        RuleFor(r => r.Name)
            .NotEmpty().WithMessage("Назва обов'язкова.")
            .MaximumLength(200).WithMessage("Назва: до 200 символів.");

        RuleFor(r => r.Unit)
            .NotEmpty().WithMessage("Одиниця виміру обов'язкова.")
            .MaximumLength(20).WithMessage("Одиниця виміру: до 20 символів.");

        RuleFor(r => r.Quantity)
            .GreaterThanOrEqualTo(0).WithMessage("Кількість не може бути від'ємною.");
    }
}

public sealed class ChangeQuantityRequestValidator : AbstractValidator<ChangeQuantityRequest>
{
    public ChangeQuantityRequestValidator()
    {
        RuleFor(r => r.Quantity)
            .GreaterThan(0).WithMessage("Кількість має бути більшою за нуль.");
    }
}
