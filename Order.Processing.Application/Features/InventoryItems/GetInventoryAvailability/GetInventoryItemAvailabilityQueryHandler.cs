using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Order.Processing.Application.Abstractions.Caching;
using Order.Processing.Application.Abstractions.Messaging;
using Order.Processing.Application.Data;
using Order.Processing.Domain.Common;

namespace Order.Processing.Application.Features.InventoryItems.GetInventoryAvailability;

public sealed class GetInventoryItemAvailabilityQueryHandler
    : IQueryHandler<GetInventoryItemsAvailabilityQuery, Result<InventoryItemAvailabilityResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly HybridCache _cache;

    public GetInventoryItemAvailabilityQueryHandler(IApplicationDbContext context, HybridCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<Result<InventoryItemAvailabilityResponse>> HandleAsync(GetInventoryItemsAvailabilityQuery query, CancellationToken cancellationToken = default)
    {
        InventoryItemAvailabilityResponse? response = await _cache.GetOrCreateAsync(
            $"inventory:{query.ProductId}",
            (context: _context, query.ProductId),
            static async (state, token) =>
            {
                var inventoryItem = await state.context.InventoryItems
                    .AsNoTracking()
                    .Where(ii => ii.ProductId == state.ProductId)
                    .FirstOrDefaultAsync(token);

                return inventoryItem is null
                    ? null
                    : new InventoryItemAvailabilityResponse(
                        inventoryItem.ProductId,
                        inventoryItem.AvailableQuantity,
                        inventoryItem.ReservedQuantity);
            },
            CacheEntries.Options,
            tags: [CacheEntries.InventoryTag],
            cancellationToken: cancellationToken);

        if (response is null)
        {
            return Result.Failure<InventoryItemAvailabilityResponse>(Error.NotFound(
                "inventory.not_found",
                $"Inventory item with ProductId {query.ProductId} not found."));
        }

        return Result.Success(response);
    }
}
