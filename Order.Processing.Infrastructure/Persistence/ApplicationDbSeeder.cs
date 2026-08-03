using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Order.Processing.Domain.Entities;

namespace Order.Processing.Infrastructure.Persistence;

public static class ApplicationDbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        using IServiceScope scope = serviceProvider.CreateScope();

        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        ILogger<ApplicationDbContext> logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        await SeedInventoryItemsAsync(context, logger, cancellationToken);
    }

    private static async Task SeedInventoryItemsAsync(
        ApplicationDbContext context,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (await context.InventoryItems.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Inventory items already seeded, skipping");
            return;
        }

        InventoryItem[] inventoryItems =
        [
            InventoryItem.Create("PRD-001", 100),
            InventoryItem.Create("PRD-002", 50),
            InventoryItem.Create("PRD-003", 25),
            InventoryItem.Create("PRD-004", 0)
        ];

        context.InventoryItems.AddRange(inventoryItems);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded {Count} inventory items", inventoryItems.Length);
    }
}
