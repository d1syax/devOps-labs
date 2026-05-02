using Microsoft.EntityFrameworkCore;
using mywebapp.Data;
using mywebapp.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

string dbHost     = builder.Configuration["db-host"]     ?? "127.0.0.1";
string dbPort     = builder.Configuration["db-port"]     ?? "3306";
string dbName     = builder.Configuration["db-name"]     ?? "mywebapp";
string dbUser     = builder.Configuration["db-user"]     ?? "mywebapp";
string dbPassword = builder.Configuration["db-password"] ?? "mywebapp";
string appHost    = builder.Configuration["host"]        ?? "127.0.0.1";
string appPort    = builder.Configuration["port"]        ?? "3000";

var connectionString = $"Server={dbHost};Port={dbPort};Database={dbName};User={dbUser};Password={dbPassword};";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, new MariaDbServerVersion(new Version(10, 11))));

builder.Host.UseSystemd();

if (Environment.GetEnvironmentVariable("LISTEN_FDS") == null)
{
    builder.WebHost.UseUrls($"http://{appHost}:{appPort}");
}

var app = builder.Build();

app.MapGet("/health/alive", () => Results.Ok("OK"));

app.MapGet("/health/ready", async (AppDbContext db) =>
{
    try
    {
        await db.Database.CanConnectAsync();
        return Results.Ok("OK");
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: $"DB unavailable: {ex.Message}", statusCode: 500);
    }
});

app.MapGet("/", (HttpContext ctx) =>
{
    if (!Wants(ctx, "text/html")) return Results.StatusCode(406);

    return Results.Content("""
        <!DOCTYPE html><html><head><title>mywebapp</title></head><body>
        <h1>Simple Inventory</h1>
        <ul>
          <li>GET /items - list all items</li>
          <li>POST /items - create item (name, quantity)</li>
          <li>GET /items/{id} — get item details</li>
        </ul>
        </body></html>
        """, "text/html");
});

app.MapGet("/items", async (AppDbContext db, HttpContext ctx) =>
{
    var items = await db.Items.OrderBy(i => i.Id).ToListAsync();

    if (Wants(ctx, "application/json"))
        return Results.Json(items.Select(i => new { i.Id, i.Name }));

    var html = new StringBuilder();
    html.Append("<!DOCTYPE html><html><body><h1>Items</h1><table border='1'><tr><th>ID</th><th>Name</th></tr>");
    foreach (var item in items)
        html.Append($"<tr><td>{item.Id}</td><td>{item.Name}</td></tr>");
    html.Append("</table></body></html>");
    return Results.Content(html.ToString(), "text/html");
});

app.MapPost("/items", async (AppDbContext db, HttpContext ctx) =>
{
    string? name = null;
    int quantity  = 0;

    if ((ctx.Request.ContentType ?? "").Contains("application/json"))
    {
        var body = await System.Text.Json.JsonSerializer.DeserializeAsync<ItemRequest>(
            ctx.Request.Body,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );
        name     = body?.Name;
        quantity = body?.Quantity ?? 0;
    }
    else
    {
        var form = await ctx.Request.ReadFormAsync();
        name     = form["name"];
        int.TryParse(form["quantity"], out quantity);
    }

    if (string.IsNullOrWhiteSpace(name))
        return Results.BadRequest("name is required");

    var item = new Item { Name = name, Quantity = quantity };
    db.Items.Add(item);
    await db.SaveChangesAsync();

    if (Wants(ctx, "application/json"))
        return Results.Json(item, statusCode: 201);

    return Results.Content(
        $"<!DOCTYPE html><html><body><p>Created: <b>{item.Name}</b> (id={item.Id})</p><a href='/items'>Back</a></body></html>",
        "text/html", statusCode: 201);
});

app.MapGet("/items/{id:int}", async (int id, AppDbContext db, HttpContext ctx) =>
{
    var item = await db.Items.FindAsync(id);
    if (item is null) return Results.NotFound("Not found");

    if (Wants(ctx, "application/json"))
        return Results.Json(item);

    return Results.Content($"""
        <!DOCTYPE html><html><body>
        <h1>Item #{item.Id}</h1>
        <table border='1'>
          <tr><th>ID</th><td>{item.Id}</td></tr>
          <tr><th>Name</th><td>{item.Name}</td></tr>
          <tr><th>Quantity</th><td>{item.Quantity}</td></tr>
          <tr><th>Created At</th><td>{item.CreatedAt:u}</td></tr>
        </table>
        <a href='/items'>Back</a>
        </body></html>
        """, "text/html");
});

app.Run();

static bool Wants(HttpContext ctx, string mime) =>
    ctx.Request.Headers["Accept"].ToString().Contains(mime);

record ItemRequest(string Name, int Quantity);