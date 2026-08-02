using Microsoft.EntityFrameworkCore;
using Order.Processing.Application.Abstractions.Messaging;
using Order.Processing.Application.Data;
using Order.Processing.Domain.Common;

namespace Order.Processing.Application.Features.InventoryItems.GetInventoryAvailability;

public sealed class GetInventoryItemAvailabilityQueryHandler
    : IQueryHandler<GetInventoryItemsAvailabilityQuery, Result<InventoryItemAvailabilityResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetInventoryItemAvailabilityQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<InventoryItemAvailabilityResponse>> HandleAsync(GetInventoryItemsAvailabilityQuery query, CancellationToken cancellationToken = default)
    {
        var inventoryItem = await _context.InventoryItems
            .AsNoTracking()
            .Where(ii => ii.ProductId == query.ProductId)
            .FirstOrDefaultAsync(cancellationToken);

        if (inventoryItem is null)
        {
            return Result.Failure<InventoryItemAvailabilityResponse>(Error.NotFound(
                "inventory.not_found",
                $"Inventory item with ProductId {query.ProductId} not found."));
        }

        var response = new InventoryItemAvailabilityResponse(
            inventoryItem.ProductId,
            inventoryItem.AvailableQuantity,
            inventoryItem.ReservedQuantity
        );

        return Result.Success(response);
    }
}
