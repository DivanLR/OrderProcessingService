using Order.Processing.Domain.Common;

namespace Order.Processing.Domain.Entities;

public class Order
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string CustomerId { get; init; }
    public List<OrderItem> Items { get; init; }
    public decimal TotalAmount { get; init; }
    public Status Status { get; private set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public static Order Create(string customerId, IEnumerable<OrderItem> items)
    {
        List<OrderItem> orderItems = [.. items];

        return new Order
        {
            CustomerId = customerId,
            Items = orderItems,
            TotalAmount = orderItems.Sum(item => item.LineTotal),
            Status = Status.Pending
        };
    }

    public Result UpdateStatus(Status status)
    {
        if (Status == status)
        {
            return Result.Success();
        }

        bool allowed = Status switch
        {
            Status.Pending => status is Status.Confirmed or Status.Cancelled,
            Status.Confirmed => status is Status.Shipped or Status.Cancelled,
            _ => false
        };

        if (!allowed)
        {
            return Result.Failure(new Error(
                "order.invalid_status_transition",
                $"An order cannot move from {Status} to {status}.",
                ErrorType.Conflict));
        }

        Status = status;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }
}

public enum Status
{
    Pending,
    Confirmed,
    Cancelled,
    Shipped
}
