using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Order.Processing.Application.Abstractions.Caching;
using Order.Processing.Application.Abstractions.Messaging;
using Order.Processing.Application.Data;
using Order.Processing.Domain.Common;

namespace Order.Processing.Application.Features.InventoryItems.Update.ReserveInventoryItem;

public sealed class ReserveInventoryItemCommandHandler : ICommandHandler<ReserveInventoryItemCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly HybridCache _cache;

    public ReserveInventoryItemCommandHandler(IApplicationDbContext dbContext, HybridCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task<Result> HandleAsync(ReserveInventoryItemCommand command, CancellationToken cancellationToken = default)
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

        var reservation = inventoryItem.Reserve(command.Quantity);

        if (reservation.IsFailure)
        {
            return reservation;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _cache.RemoveByTagAsync(CacheEntries.InventoryTag, cancellationToken);

        return Result.Success();
    }
}
