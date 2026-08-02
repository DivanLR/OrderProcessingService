using Order.Processing.Application.Abstractions.Messaging;
using Order.Processing.Domain.Common;

namespace Order.Processing.Application.Features.InventoryItems.GetInventoryAvailability;

public sealed record GetInventoryItemsAvailabilityQuery(string ProductId)
    : IQuery<Result<InventoryItemAvailabilityResponse>>;

public sealed record InventoryItemAvailabilityResponse(string ProductId, int AvailableQuantity, int ReservedQuantity);
