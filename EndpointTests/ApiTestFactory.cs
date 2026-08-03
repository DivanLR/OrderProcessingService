using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Order.Processing.Application.Data;
using Order.Processing.Infrastructure.Persistence;

namespace Order.Processing.Api.Tests;

public sealed class ApiTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();

        using IServiceScope scope = Services.CreateScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await context.Database.EnsureCreatedAsync();
        await ApplicationDbSeeder.SeedAsync(Services);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));
            services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        });
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _connection.DisposeAsync();
        await DisposeAsync();
    }
}
