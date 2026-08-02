using Order.Processing.Application.Abstractions.Messaging;

namespace Order.Processing.Application.Features.InventoryItems.Update.ReleaseReservedItem;

public sealed record ReleaseInventoryItemCommand(string ProductId, int Quantity) : ICommand;
