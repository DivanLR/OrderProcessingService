using Order.Processing.Domain.Common;

namespace Order.Processing.Domain.Entities;

public sealed class InventoryItem
{
    public required string ProductId { get; init; }
    public int AvailableQuantity { get; private set; }
    public int ReservedQuantity { get; private set; }

    public static InventoryItem Create(string productId, int availableQuantity) =>
        new() { ProductId = productId, AvailableQuantity = availableQuantity };

    public Result Reserve(int quantity)
    {
        AvailableQuantity -= quantity;
        ReservedQuantity += quantity;

        return Result.Success();
    }

    public Result Release(int quantity)
    {
        AvailableQuantity += quantity;
        ReservedQuantity -= quantity;

        return Result.Success();
    }
}
