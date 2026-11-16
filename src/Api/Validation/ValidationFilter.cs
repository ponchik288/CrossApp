using FluentValidation;
using FluentValidation.Results;

namespace Api.Validation;

// Фільтр маршруту: перевіряє тіло запиту ДО виклику обробника.
// Якщо є помилки — одразу 400 з переліком полів, обробник не викликається.
public sealed class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        T? model = context.Arguments.OfType<T>().FirstOrDefault();
        if (model is null)
        {
            return TypedResults.ValidationProblem(
                new Dictionary<string, string[]> { ["body"] = ["Тіло запиту обов'язкове."] });
        }

        ValidationResult result = await validator.ValidateAsync(model, context.HttpContext.RequestAborted);

        return result.IsValid
            ? await next(context)
            : TypedResults.ValidationProblem(result.ToDictionary());
    }
}
