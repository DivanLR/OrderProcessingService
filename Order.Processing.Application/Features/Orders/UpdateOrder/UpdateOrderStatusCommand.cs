using Order.Processing.Application.Abstractions.Messaging;

namespace Order.Processing.Application.Features.Orders.UpdateOrder;

public sealed record UpdateOrderStatusCommand(Guid OrderId, string Status) : ICommand;

public sealed record UpdateOrderStatusRequest(string Status);
