using Order.Processing.Application.Abstractions.Messaging;

namespace Order.Processing.Application.Features.InventoryItems.Update.ReserveInventoryItem;

public sealed record ReserveInventoryItemCommand(string ProductId, int Quantity) : ICommand;
