using System;
using System.Collections.Generic;
using System.Text;

namespace Order.Processing.Domain.Entities;

public class Order
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string CustomerId { get; init; }
    public List<InventoryItem> Items { get; init; }
    public decimal TotalAmount { get; init; }
    public Status Status { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

public enum Status
{
    Pending,
    Confirmed,
    Cancelled,
    Shipped
}
