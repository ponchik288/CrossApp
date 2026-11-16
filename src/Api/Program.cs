using System.Text.Encodings.Web;
using Api.Contracts;
using Api.Endpoints;
using Api.Errors;
using Api.Validation;
using Core;
using Core.Abstractions;
using Core.Services;
using Core.Storage;
using FluentValidation;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();                               // формат помилок RFC 9457
builder.Services.AddExceptionHandler<DomainExceptionHandler>();     // DomainException -> 409

// Кирилиця у відповідях як є, а не \uXXXX.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping);

// Сховище і сервіс — Singleton: дані в пам'яті мають жити між запитами.
builder.Services.AddSingleton<ICatalogStore>(_ => new InMemoryCatalogStore(SampleData.Products()));
builder.Services.AddSingleton<CatalogService>(provider =>
{
    var service = new CatalogService(provider.GetRequiredService<ICatalogStore>());
    ILogger logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Catalog");

    // Той самий механізм подій, що й у Cli (ЛР6): тут підписник — логер Api.
    service.Changed += (_, e) =>
        logger.LogInformation("{Kind} {Id}: {Delta:+0;-0} -> залишок {QuantityAfter}", e.Kind, e.Id, e.Delta, e.QuantityAfter);

    return service;
});

builder.Services.AddScoped<IValidator<SaveProductRequest>, SaveProductRequestValidator>();
builder.Services.AddScoped<IValidator<ChangeQuantityRequest>, ChangeQuantityRequestValidator>();

var app = builder.Build();

app.UseExceptionHandler();      // виняток не показує клієнтові stack trace
app.UseStatusCodePages();       // 404/405 без тіла теж у форматі ProblemDetails

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();               // документ: /openapi/v1.json
    app.MapScalarApiReference();    // UI: /scalar/v1
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapProducts();

app.Run();

// Потрібно для інтеграційних тестів (WebApplicationFactory<Program>).
public partial class Program;
