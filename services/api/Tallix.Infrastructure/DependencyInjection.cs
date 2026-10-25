using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tallix.Application.Contacts;
using Tallix.Application.Ledger;
using Tallix.Application.Users;
using Tallix.Infrastructure.Persistence;

namespace Tallix.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<TallixDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IContactRepository, ContactRepository>();
        services.AddScoped<ILedgerRepository, LedgerRepository>();

        return services;
    }
}