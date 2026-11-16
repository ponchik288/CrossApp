using Core;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Api.Errors;

// Порушення бізнес-правила (DomainException) -> 409 Conflict у форматі ProblemDetails.
// Некоректний аргумент або тіло запиту -> 400. Усе інше лишається 500 без подробиць для клієнта.
internal sealed class DomainExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        (int status, string title)? mapped = exception switch
        {
            DomainException => (StatusCodes.Status409Conflict, "Порушено бізнес-правило."),
            // тіло запиту не розібралося (порожнє, зламаний JSON, не той тип поля)
            BadHttpRequestException bad => (bad.StatusCode, "Некоректний запит."),
            ArgumentException => (StatusCodes.Status400BadRequest, "Некоректні дані запиту."),
            _ => null
        };

        if (mapped is null)
            return false;       // не наш випадок: спрацює стандартний обробник (500 без stack trace)

        httpContext.Response.StatusCode = mapped.Value.status;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = mapped.Value.status,
                Title = mapped.Value.title,
                // текст внутрішнього винятку фреймворку клієнтові не показуємо
                Detail = exception is BadHttpRequestException
                    ? "Тіло запиту відсутнє або не є коректним JSON потрібної форми."
                    : exception.Message.ReplaceLineEndings(" ")
            }
        });
    }
}
