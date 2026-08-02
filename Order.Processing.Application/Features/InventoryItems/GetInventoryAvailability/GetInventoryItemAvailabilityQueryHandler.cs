using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Order.Processing.Application.Data;
using Order.Processing.Domain.Common;

namespace Order.Processing.Application.Features.InventoryItems.GetInventoryAvailability;

public sealed class GetInventoryItemAvailabilityQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetInventoryItemAvailabilityQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<InventoryItemAvailabilityResponse>> HandleAsync(GetInventoryItemsAvailabilityQuery query, CancellationToken cancellationToken = default)
    {
        var inventoryItem = await _context.InventoryItems
            .Where(ii => ii.ProductId == query.ProductId)
            .FirstOrDefaultAsync(cancellationToken);

        if (inventoryItem is null)
        {
            return Result<InventoryItemAvailabilityResponse>.Failure(Error.NotFound($"Inventory item with ProductId {query.ProductId} not found."));
        }

        var response = new InventoryItemAvailabilityResponse(
            inventoryItem.ProductId,
            inventoryItem.AvailableQuantity,
            inventoryItem.ReservedQuantity
        );

        return Result<InventoryItemAvailabilityResponse>.Success(response);
    }
}
