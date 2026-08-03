namespace Order.Processing.Domain.Entities;

public sealed class OrderItem
{
    public required string ProductId { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }

    public decimal LineTotal => Quantity * UnitPrice;
}
