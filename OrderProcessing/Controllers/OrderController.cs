using Microsoft.AspNetCore.Mvc;
using Order.Processing.Application.Abstractions.Messaging;
using Order.Processing.Application.Features.Orders.CreateOrder;
using Order.Processing.Application.Features.Orders.GetAllOrders;
using Order.Processing.Application.Features.Orders.GetOrder;
using Order.Processing.Application.Features.Orders.UpdateOrder;
using Order.Processing.Domain.Common;

namespace OrderProcessing.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrderController : ControllerBase
{
    private readonly ICommandHandler<CreateOrderCommand, Result<CreateOrderResponse>> _createOrder;
    private readonly IQueryHandler<GetOrderQuery, Result<OrderResponse>> _getOrder;
    private readonly IQueryHandler<GetAllOrdersQuery, Result<OrdersPageResponse>> _getAllOrders;
    private readonly ICommandHandler<UpdateOrderStatusCommand> _updateOrderStatus;

    public OrderController(
        ICommandHandler<CreateOrderCommand, Result<CreateOrderResponse>> createOrder,
        IQueryHandler<GetOrderQuery, Result<OrderResponse>> getOrder,
        IQueryHandler<GetAllOrdersQuery, Result<OrdersPageResponse>> getAllOrders,
        ICommandHandler<UpdateOrderStatusCommand> updateOrderStatus)
    {
        _createOrder = createOrder;
        _getOrder = getOrder;
        _getAllOrders = getAllOrders;
        _updateOrderStatus = updateOrderStatus;
    }

    [HttpPost]
    [ProducesResponseType<CreateOrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<Result<CreateOrderResponse>> Create(
        CreateOrderCommand command,
        CancellationToken cancellationToken)
    {
        var response = await _createOrder.HandleAsync(command, cancellationToken);

        return response;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<Result<OrderResponse>> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await _getOrder.HandleAsync(new GetOrderQuery(id), cancellationToken);

        return response;
    }

    [HttpGet]
    [ProducesResponseType<OrdersPageResponse>(StatusCodes.Status200OK)]
    public async Task<Result<OrdersPageResponse>> GetAll(
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var response = await _getAllOrders.HandleAsync(
            new GetAllOrdersQuery(page, pageSize),
            cancellationToken);

        return response;
    }

    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<Result> UpdateStatus(
        Guid id,
        UpdateOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _updateOrderStatus.HandleAsync(
            new UpdateOrderStatusCommand(id, request.Status),
            cancellationToken);

        return response;
    }
}
