using Api.Contracts;
using Api.Validation;
using Core.Abstractions;
using Core.Domain;
using Core.Reports;
using Core.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints;

// Маршрути товарів. Читання — напряму зі сховища; зміни — через CatalogService,
// бо він перевіряє правила і піднімає події (їх записує логер).
public static class ProductEndpoints
{
    public static void MapProducts(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/products").WithTags("Products");

        // GET /api/products?page=1&pageSize=20&search=цем
        group.MapGet("/", (ICatalogStore store, int page = 1, int pageSize = 50, string? search = null) =>
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            IEnumerable<Product> query = store.List();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p =>
                    p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    p.Sku.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            List<Product> found = query.OrderBy(p => p.Sku, StringComparer.Ordinal).ToList();
            List<ProductDto> items = found.Skip((page - 1) * pageSize).Take(pageSize).Select(ProductDto.From).ToList();

            return TypedResults.Ok(new PagedResult<ProductDto>(items, found.Count, page, pageSize));
        });

        // GET /api/products/low-stock?threshold=5
        group.MapGet("/low-stock", (ICatalogStore store, int threshold = 10) =>
            TypedResults.Ok(ReportService.LowStock(store.List(), threshold)));

        // GET /api/products/{id}
        group.MapGet("/{id}", Results<Ok<ProductDto>, NotFound> (string id, ICatalogStore store) =>
            store.GetById(id) is { } found
                ? TypedResults.Ok(ProductDto.From(found))
                : TypedResults.NotFound());

        // POST /api/products
        group.MapPost("/", Results<Created<ProductDto>, Conflict<ProblemDetails>> (
            SaveProductRequest request, ICatalogStore store, CatalogService service) =>
        {
            if (FindBySku(store, request.Sku) is not null)
                return TypedResults.Conflict(SkuTaken(request.Sku));

            Product created = service.Add(request.Sku, request.Name, request.Unit, request.Quantity);
            return TypedResults.Created($"/api/products/{created.Id}", ProductDto.From(created));
        })
        .AddEndpointFilter<ValidationFilter<SaveProductRequest>>()
        .ProducesValidationProblem();

        // PUT /api/products/{id}
        group.MapPut("/{id}", Results<Ok<ProductDto>, NotFound, Conflict<ProblemDetails>> (
            string id, SaveProductRequest request, ICatalogStore store, CatalogService service) =>
        {
            if (store.GetById(id) is null)
                return TypedResults.NotFound();

            // артикул має лишатися унікальним: інший товар з таким SKU — конфлікт
            if (FindBySku(store, request.Sku) is { } other && !other.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
                return TypedResults.Conflict(SkuTaken(request.Sku));

            Product replaced = service.Replace(id, request.Sku, request.Name, request.Unit, request.Quantity);
            return TypedResults.Ok(ProductDto.From(replaced));
        })
        .AddEndpointFilter<ValidationFilter<SaveProductRequest>>()
        .ProducesValidationProblem();

        // DELETE /api/products/{id}
        group.MapDelete("/{id}", Results<NoContent, NotFound> (string id, CatalogService service) =>
            service.Remove(id)
                ? TypedResults.NoContent()
                : TypedResults.NotFound());

        // POST /api/products/{id}/receive і /issue — прихід і видача
        group.MapPost("/{id}/receive", Results<Ok<ProductDto>, NotFound> (
            string id, ChangeQuantityRequest request, ICatalogStore store, CatalogService service) =>
        {
            if (store.GetById(id) is null)
                return TypedResults.NotFound();

            service.Receive(id, request.Quantity);
            return TypedResults.Ok(ProductDto.From(store.GetById(id)!));
        })
        .AddEndpointFilter<ValidationFilter<ChangeQuantityRequest>>()
        .ProducesValidationProblem();

        group.MapPost("/{id}/issue", Results<Ok<ProductDto>, NotFound> (
            string id, ChangeQuantityRequest request, ICatalogStore store, CatalogService service) =>
        {
            if (store.GetById(id) is null)
                return TypedResults.NotFound();

            // Якщо залишку не вистачає, сервіс кине DomainException -> обробник винятків поверне 409.
            service.Issue(id, request.Quantity);
            return TypedResults.Ok(ProductDto.From(store.GetById(id)!));
        })
        .AddEndpointFilter<ValidationFilter<ChangeQuantityRequest>>()
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static Product? FindBySku(ICatalogStore store, string sku) =>
        store.List().FirstOrDefault(p => p.Sku.Equals(sku.Trim(), StringComparison.OrdinalIgnoreCase));

    private static ProblemDetails SkuTaken(string sku) => new()
    {
        Status = StatusCodes.Status409Conflict,
        Title = "Порушено бізнес-правило.",
        Detail = $"Товар з артикулом {sku.Trim().ToUpperInvariant()} уже існує."
    };
}
