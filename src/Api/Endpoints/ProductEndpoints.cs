using Api.Validation;
using Contracts;
using Core.Abstractions;
using Core.Domain;
using Core.Reports;
using Core.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Api.Endpoints;

// Маршрути товарів. Читання — напряму зі сховища; зміни — через CatalogService,
// бо він перевіряє правила і піднімає події (їх записує логер).
public static class ProductEndpoints
{
    public static void MapProducts(this WebApplication app, string prefix, string tag)
    {
        RouteGroupBuilder group = app.MapGroup(prefix).WithTags(tag);

        // GET /api/products?page=1&pageSize=20&search=цем&sortBy=quantity&desc=true
        group.MapGet("/", (ICatalogStore store, int page = 1, int pageSize = 50, string? search = null,
            string sortBy = "sku", bool desc = false) =>
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

            // Сортування на боці сервера; другий ключ (SKU) робить порядок однозначним.
            IOrderedEnumerable<Product> ordered = (sortBy.ToLowerInvariant(), desc) switch
            {
                ("name", false) => query.OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase),
                ("name", true) => query.OrderByDescending(p => p.Name, StringComparer.CurrentCultureIgnoreCase),
                ("quantity", false) => query.OrderBy(p => p.Quantity),
                ("quantity", true) => query.OrderByDescending(p => p.Quantity),
                (_, true) => query.OrderByDescending(p => p.Sku, StringComparer.Ordinal),
                _ => query.OrderBy(p => p.Sku, StringComparer.Ordinal)
            };

            List<Product> found = ordered.ThenBy(p => p.Sku, StringComparer.Ordinal).ToList();
            List<ProductDto> items = found.Skip((page - 1) * pageSize).Take(pageSize).Select(p => p.ToContract()).ToList();

            return TypedResults.Ok(new PagedResult<ProductDto>(items, found.Count, page, pageSize));
        });

        // GET /api/products/low-stock?threshold=5
        group.MapGet("/low-stock", (ICatalogStore store, int threshold = 10) =>
            TypedResults.Ok(ReportService.LowStock(store.List(), threshold)));

        // GET /api/products/{id}
        group.MapGet("/{id}", Results<Ok<ProductDto>, ProblemHttpResult> (string id, ICatalogStore store) =>
            store.GetById(id) is { } found
                ? TypedResults.Ok(found.ToContract())
                : NotFound(id));

        // POST /api/products
        group.MapPost("/", Results<Created<ProductDto>, ProblemHttpResult> (
            SaveProductRequest request, ICatalogStore store, CatalogService service) =>
        {
            if (FindBySku(store, request.Sku) is not null)
                return SkuTaken(request.Sku);

            Product created = service.Add(request.Sku, request.Name, request.Unit, request.Quantity);
            return TypedResults.Created($"{prefix}/{created.Id}", created.ToContract());
        })
        .AddEndpointFilter<ValidationFilter<SaveProductRequest>>()
        .ProducesValidationProblem();

        // PUT /api/products/{id}
        group.MapPut("/{id}", Results<Ok<ProductDto>, ProblemHttpResult> (
            string id, SaveProductRequest request, ICatalogStore store, CatalogService service) =>
        {
            if (store.GetById(id) is null)
                return NotFound(id);

            // артикул має лишатися унікальним: інший товар з таким SKU — конфлікт
            if (FindBySku(store, request.Sku) is { } other && !other.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
                return SkuTaken(request.Sku);

            Product replaced = service.Replace(id, request.Sku, request.Name, request.Unit, request.Quantity);
            return TypedResults.Ok(replaced.ToContract());
        })
        .AddEndpointFilter<ValidationFilter<SaveProductRequest>>()
        .ProducesValidationProblem();

        // DELETE /api/products/{id}
        group.MapDelete("/{id}", Results<NoContent, ProblemHttpResult> (string id, CatalogService service) =>
            service.Remove(id)
                ? TypedResults.NoContent()
                : NotFound(id));

        // GET /api/products/{id}/batches — журнал надходжень товару
        group.MapGet("/{id}/batches", Results<Ok<IReadOnlyList<BatchRecord>>, ProblemHttpResult> (
            string id, ICatalogStore store, IBatchJournal journal) =>
            store.GetById(id) is null
                ? NotFound(id)
                : TypedResults.Ok(journal.ForProduct(id)));

        // POST /api/products/{id}/receive і /issue — прихід і видача
        group.MapPost("/{id}/receive", Results<Ok<ProductDto>, ProblemHttpResult> (
            string id, ChangeQuantityRequest request, ICatalogStore store, CatalogService service, IBatchJournal journal) =>
        {
            if (store.GetById(id) is null)
                return NotFound(id);

            service.Receive(id, request.Quantity);
            journal.Record(id, request.Quantity);       // партія в таблиці Batches (зв'язок 1:N з Products)
            return TypedResults.Ok(store.GetById(id)!.ToContract());
        })
        .AddEndpointFilter<ValidationFilter<ChangeQuantityRequest>>()
        .ProducesValidationProblem();

        group.MapPost("/{id}/issue", Results<Ok<ProductDto>, ProblemHttpResult> (
            string id, ChangeQuantityRequest request, ICatalogStore store, CatalogService service) =>
        {
            if (store.GetById(id) is null)
                return NotFound(id);

            // Якщо залишку не вистачає, сервіс кине DomainException -> обробник винятків поверне 409.
            service.Issue(id, request.Quantity);
            return TypedResults.Ok(store.GetById(id)!.ToContract());
        })
        .AddEndpointFilter<ValidationFilter<ChangeQuantityRequest>>()
        .ProducesValidationProblem();
    }

    private static Product? FindBySku(ICatalogStore store, string sku) =>
        store.List().FirstOrDefault(p => p.Sku.Equals(sku.Trim(), StringComparison.OrdinalIgnoreCase));

    private static ProblemHttpResult NotFound(string id) => TypedResults.Problem(
        statusCode: StatusCodes.Status404NotFound,
        title: "Товар не знайдено.",
        detail: $"Товару з id {id} немає.");

    private static ProblemHttpResult SkuTaken(string sku) => TypedResults.Problem(
        statusCode: StatusCodes.Status409Conflict,
        title: "Порушено бізнес-правило.",
        detail: $"Товар з артикулом {sku.Trim().ToUpperInvariant()} уже існує.");
}
