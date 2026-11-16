using System.Text.Encodings.Web;
using Api.Contracts;
using Api.Endpoints;
using Api.Errors;
using Api.Validation;
using Core.Abstractions;
using Core.Services;
using FluentValidation;
using Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();                               // формат помилок RFC 9457
builder.Services.AddExceptionHandler<DomainExceptionHandler>();     // DomainException -> 409

// Кирилиця у відповідях як є, а не \uXXXX.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping);

// Сховище тепер на EF Core + SQLite (ЛР11). DbContext, сховище і сервіс — Scoped: свої на кожен HTTP-запит.
builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("AppDb"), builder.Environment.ContentRootPath);

builder.Services.AddScoped<CatalogService>(provider =>
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

// У Development схема БД оновлюється під час запуску; у Production — командою dotnet ef database update.
if (app.Environment.IsDevelopment())
    app.Services.ApplyMigrations();

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
