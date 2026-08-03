using Order.Processing.Application.Abstractions.Messaging;
using Order.Processing.Domain.Common;
using Order.Processing.Domain.Entities;

namespace Order.Processing.Application.Features.Orders.CreateOrder;

public sealed record CreateOrderCommand(string CustomerId, IReadOnlyCollection<CreateOrderItem> Items)
    : ICommand<Result<CreateOrderResponse>>;

public sealed record CreateOrderItem(string ProductId, int Quantity, decimal UnitPrice);

public sealed record CreateOrderResponse(
    Guid OrderId,
    decimal TotalAmount,
    Status Status,
    DateTime CreatedAt);
