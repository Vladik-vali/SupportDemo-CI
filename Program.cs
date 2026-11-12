using Npgsql;
using SupportDemo;

var builder = WebApplication.CreateBuilder(args);

// The host/container provides ConnectionStrings__DefaultConnection.
// Database connectivity is not required to serve GET /health.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    // Fallback is for local development only; never contains a password.
    connectionString = "Host=localhost;Port=5432;Database=supportdb;Username=supportuser";
}

builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));
builder.Services.AddSingleton<TicketRepository>();

var app = builder.Build();

// This endpoint intentionally does not depend on PostgreSQL.
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "SupportDemo" }));

app.MapGet("/health/db", async (NpgsqlDataSource dataSource, CancellationToken ct) =>
{
    try
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("SELECT 1", connection);
        await command.ExecuteScalarAsync(ct);
        return Results.Ok(new { status = "Healthy", database = "PostgreSQL" });
    }
    catch (Exception ex) when (ex is NpgsqlException or TimeoutException or InvalidOperationException)
    {
        return Results.Json(new { status = "Unhealthy", database = "PostgreSQL" }, statusCode: 503);
    }
});

// Schema initialization is safe to execute repeatedly; it never deletes data.
// Transient failures (e.g. DB not yet ready) are retried on requests.
app.MapPost("/api/tickets", async (CreateTicketRequest request, TicketRepository repo, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Description))
        return Results.BadRequest(new { error = "title and description are required" });

    if (request.Title.Length > 200 || request.Description.Length > 4000)
        return Results.BadRequest(new { error = "title max 200, description max 4000 characters" });

    var ticket = await repo.CreateAsync(request.Title.Trim(), request.Description.Trim(), ct);
    return Results.Created($"/api/tickets/{ticket.Id}", ticket);
});

app.MapGet("/api/tickets", async (TicketRepository repo, CancellationToken ct) =>
    Results.Ok(await repo.GetAllAsync(ct)));

app.MapGet("/api/tickets/{id:int}", async (int id, TicketRepository repo, CancellationToken ct) =>
{
    var ticket = await repo.GetByIdAsync(id, ct);
    return ticket is null ? Results.NotFound() : Results.Ok(ticket);
});

app.MapPatch("/api/tickets/{id:int}/status", async (int id, UpdateTicketStatusRequest request, TicketRepository repo, CancellationToken ct) =>
{
    if (!TicketStatuses.IsValid(request.Status))
        return Results.BadRequest(new { error = "status must be New, InProgress or Closed" });

    var ticket = await repo.UpdateStatusAsync(id, request.Status!, ct);
    return ticket is null ? Results.NotFound() : Results.Ok(ticket);
});

app.Run();
