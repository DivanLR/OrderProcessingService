using Microsoft.EntityFrameworkCore;
using Order.Processing.Application.Abstractions.Messaging;
using Order.Processing.Application.Data;
using Order.Processing.Domain.Common;

namespace Order.Processing.Application.Features.InventoryItems.Update.ReserveInventoryItem;

public sealed class ReserveInventoryItemCommandHandler : ICommandHandler<ReserveInventoryItemCommand>
{
    private readonly IApplicationDbContext _dbContext;

    public ReserveInventoryItemCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
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

        return Result.Success();
    }
}
