using System;
using System.Collections.Generic;
using System.Text;
using Order.Processing.Application.Abstractions.Messaging;

namespace Order.Processing.Application.Features.InventoryItems.GetInventoryAvailability;

public sealed record GetInventoryItemsAvailabilityQuery(string ProductId) : IQuery<InventoryItemAvailabilityResponse>;

public sealed record InventoryItemAvailabilityResponse(string ProductId, int AvailableQuantity, int ReservedQuantity);
