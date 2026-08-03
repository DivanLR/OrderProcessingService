using Order.Processing.Application.Abstractions.Messaging;
using Order.Processing.Domain.Common;
using Order.Processing.Domain.Entities;

namespace Order.Processing.Application.Features.Orders.GetOrder;

public sealed record GetOrderQuery(Guid OrderId) : IQuery<Result<OrderResponse>>;

public sealed record OrderResponse(
    Guid OrderId,
    string CustomerId,
    IReadOnlyCollection<OrderItemResponse> Items,
    decimal TotalAmount,
    Status Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record OrderItemResponse(string ProductId, int Quantity, decimal UnitPrice);
