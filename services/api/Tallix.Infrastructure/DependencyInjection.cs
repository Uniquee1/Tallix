using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

        return services;
    }
}