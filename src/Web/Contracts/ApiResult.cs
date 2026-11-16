namespace Web.Contracts;

// Результат виклику Api для форми: успіх або повідомлення і (для 400) помилки по полях.
public sealed record ApiResult(bool Ok, string? Message = null, Dictionary<string, string[]>? Errors = null);

// Тіло помилки Api у форматі ProblemDetails / ValidationProblem.
public sealed record ValidationProblemResponse(string? Title, string? Detail, Dictionary<string, string[]>? Errors);
