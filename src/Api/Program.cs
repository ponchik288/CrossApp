using System.Text.Encodings.Web;
using System.Text.Json;
using Api.Endpoints;
using Api.Errors;
using Api.Health;
using Api.Logging;
using Api.Validation;
using Contracts;
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
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;    // стиль імен JSON задано явно
    options.SerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
});

// Сховище тепер на EF Core + SQLite (ЛР11). DbContext, сховище і сервіс — Scoped: свої на кожен HTTP-запит.
builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("AppDb"), builder.Environment.ContentRootPath);

builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

builder.Services.AddScoped<CatalogService>(provider =>
{
    var service = new CatalogService(provider.GetRequiredService<ICatalogStore>());
    ILogger logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Catalog");

    // Той самий механізм подій, що й у Cli (ЛР6): тут підписник — логер Api.
    service.Changed += (_, e) => CatalogLog.CatalogChanged(logger, e.Kind, e.Id, e.Delta, e.QuantityAfter);

    return service;
});

// CORS: дозволяє браузерному клієнту з іншого origin (наприклад, Blazor WebAssembly) викликати Api.
// Для Blazor Interactive Server не потрібна: там запит до Api робить сервер Web, а не браузер.
string webOrigin = builder.Configuration["Cors:WebOrigin"] ?? "http://localhost:5090";
builder.Services.AddCors(options => options.AddPolicy("web", policy =>
    policy.WithOrigins(webOrigin).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddScoped<IValidator<SaveProductRequest>, SaveProductRequestValidator>();
builder.Services.AddScoped<IValidator<ChangeQuantityRequest>, ChangeQuantityRequestValidator>();

var app = builder.Build();

// Міграції під час запуску: завжди в Development, а в інших середовищах — якщо це ввімкнено
// налаштуванням Database:MigrateOnStartup (так запускається контейнер, див. docker-compose.yml).
if (app.Environment.IsDevelopment() || app.Configuration.GetValue("Database:MigrateOnStartup", false))
    app.Services.ApplyMigrations();

app.UseExceptionHandler();      // виняток не показує клієнтові stack trace
app.UseStatusCodePages();       // 404/405 без тіла теж у форматі ProblemDetails

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();               // документ: /openapi/v1.json
    app.MapScalarApiReference();    // UI: /scalar/v1
}

app.UseCors("web");             // обов'язково до маршрутів

app.MapHealthChecks("/health");     // 200 "Healthy", якщо застосунок працює і база відкривається
// Версія контракту в адресі. /api/products лишається як псевдонім v1 для клієнтів, написаних до версіонування.
app.MapProducts("/api/v1/products", "Products v1");
app.MapProducts("/api/products", "Products (без версії, те саме що v1)");

app.Run();

// Потрібно для інтеграційних тестів (WebApplicationFactory<Program>).
public partial class Program;
