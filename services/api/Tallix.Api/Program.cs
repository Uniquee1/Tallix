using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Tallix.Api.Auth;
using Tallix.Api.Errors;
using Tallix.Application.Contacts;
using Tallix.Application.Ledger;
using Tallix.Application.Users;
using Tallix.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration["DATABASE_CONNECTION_STRING"]
    ?? throw new InvalidOperationException(
        "The DATABASE_CONNECTION_STRING environment variable is not set.");

var authIssuer = builder.Configuration["AUTH_ISSUER"]
    ?? throw new InvalidOperationException(
        "The AUTH_ISSUER environment variable is not set.");

var authAudience = builder.Configuration["AUTH_AUDIENCE"]
    ?? throw new InvalidOperationException(
        "The AUTH_AUDIENCE environment variable is not set.");

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // Enums travel as text in the form the requirements use (LENT, PAYMENT_RECEIVED),
        // never as numbers.
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper, allowIntegerValues: false));
    });

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddInfrastructure(connectionString);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<GetOrCreateCurrentUser>();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
builder.Services.AddScoped<CreateContact>();
builder.Services.AddScoped<GetContact>();
builder.Services.AddScoped<ListContacts>();
builder.Services.AddScoped<UpdateContact>();
builder.Services.AddScoped<DeleteContact>();
builder.Services.AddScoped<RecordLedgerEntry>();
builder.Services.AddScoped<GetLedgerEntry>();
builder.Services.AddScoped<GetContactLedger>();
builder.Services.AddScoped<ReverseLedgerEntry>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = authIssuer;
        options.Audience = authAudience;
        options.MapInboundClaims = false;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();