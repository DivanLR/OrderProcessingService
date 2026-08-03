using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Order.Processing.Application.Data;
using Order.Processing.Infrastructure.Persistence;

namespace Order.Processing.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        string connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Connection string 'Database' is not configured.");

        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        return services;
    }

    public static async Task InitialiseDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        using (IServiceScope scope = services.CreateScope())
        {
            ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await context.Database.EnsureCreatedAsync(cancellationToken);
        }

        await ApplicationDbSeeder.SeedAsync(services, cancellationToken);
    }
}
