using Tallix.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration["DATABASE_CONNECTION_STRING"]
    ?? throw new InvalidOperationException(
        "The DATABASE_CONNECTION_STRING environment variable is not set.");

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddInfrastructure(connectionString);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();