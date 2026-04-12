using OpenClawNet.Gateway.Endpoints;
using OpenClawNet.Gateway.Hubs;
using OpenClawNet.Models.Abstractions;
using OpenClawNet.Models.Ollama;
using OpenClawNet.Storage;

var builder = WebApplication.CreateBuilder(args);

// Aspire service defaults
builder.AddServiceDefaults();

// OpenAPI
builder.Services.AddOpenApi();

// SignalR for real-time streaming
builder.Services.AddSignalR();

// CORS for Blazor Web UI
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// Storage (SQLite + EF Core)
builder.Services.AddOpenClawStorage(
    builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=openclawnet.db");

// Model provider (Ollama by default)
builder.Services.AddModelClient<OllamaModelClient>();
builder.Services.Configure<ModelOptions>(builder.Configuration.GetSection("Model"));

var app = builder.Build();

// Aspire default endpoints
app.MapDefaultEndpoints();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<Microsoft.EntityFrameworkCore.IDbContextFactory<OpenClawNet.Storage.OpenClawDbContext>>();
    await using var db = await dbFactory.CreateDbContextAsync();
    await db.Database.EnsureCreatedAsync();
}

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// API version
app.MapGet("/api/version", () => Results.Ok(new { version = "0.1.0", name = "OpenClawNet" }));

// Map API endpoints
app.MapChatEndpoints();
app.MapSessionEndpoints();

// Map SignalR hub
app.MapHub<ChatHub>("/hubs/chat");

app.Run();
