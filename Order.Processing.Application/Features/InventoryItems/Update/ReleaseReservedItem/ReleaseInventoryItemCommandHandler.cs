using Microsoft.EntityFrameworkCore;
using Order.Processing.Application.Abstractions.Messaging;
using Order.Processing.Application.Data;
using Order.Processing.Domain.Common;

namespace Order.Processing.Application.Features.InventoryItems.Update.ReleaseReservedItem;

public sealed class ReleaseInventoryItemCommandHandler : ICommandHandler<ReleaseInventoryItemCommand>
{
    private readonly IApplicationDbContext _dbContext;

    public ReleaseInventoryItemCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> HandleAsync(ReleaseInventoryItemCommand command, CancellationToken cancellationToken = default)
    {
        var inventoryItem = await _dbContext.InventoryItems
            .Where(ii => ii.ProductId == command.ProductId)
            .FirstOrDefaultAsync(cancellationToken);

        if (inventoryItem is null)
        {
            return Result.Failure(Error.NotFound(
                "inventory.not_found",
                $"Inventory item with ProductId '{command.ProductId}' not found."));
        }

        var release = inventoryItem.Release(command.Quantity);

        if (release.IsFailure)
        {
            return release;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
