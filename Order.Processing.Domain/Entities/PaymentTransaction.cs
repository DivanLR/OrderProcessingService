using System;
using System.Collections.Generic;
using System.Text;

namespace Order.Processing.Domain.Entities;

public class PaymentTransaction
{
    public Guid TransactionId { get; init; } = Guid.NewGuid();
    public string OrderId { get; init; }
    public decimal Amount { get; init; }
    public PaymentStatus Status { get; init; }
    public DateTime ProcessedAt { get; init; }
}

public enum PaymentStatus
{
    Pending,
    Completed,
    Failed
}

