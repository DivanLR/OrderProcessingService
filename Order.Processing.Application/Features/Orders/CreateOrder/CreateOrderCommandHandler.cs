using Microsoft.Extensions.Caching.Hybrid;
using Order.Processing.Application.Abstractions.Caching;
using Order.Processing.Application.Abstractions.Messaging;
using Order.Processing.Application.Data;
using Order.Processing.Domain.Common;
using Order.Processing.Domain.Entities;

namespace Order.Processing.Application.Features.Orders.CreateOrder;

public sealed class CreateOrderCommandHandler
    : ICommandHandler<CreateOrderCommand, Result<CreateOrderResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly HybridCache _cache;

    public CreateOrderCommandHandler(IApplicationDbContext dbContext, HybridCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task<Result<CreateOrderResponse>> HandleAsync(CreateOrderCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.CustomerId))
        {
            return Result.Failure<CreateOrderResponse>(Error.Validation("CustomerId is required."));
        }

        if (command.Items is null || command.Items.Count == 0)
        {
            return Result.Failure<CreateOrderResponse>(Error.Validation("An order requires at least one item."));
        }

        foreach (CreateOrderItem item in command.Items)
        {
            if (string.IsNullOrWhiteSpace(item.ProductId))
            {
                return Result.Failure<CreateOrderResponse>(Error.Validation("ProductId is required for every item."));
            }

            if (item.Quantity <= 0)
            {
                return Result.Failure<CreateOrderResponse>(Error.Validation(
                    $"Quantity for product '{item.ProductId}' must be greater than zero."));
            }

            if (item.UnitPrice < 0)
            {
                return Result.Failure<CreateOrderResponse>(Error.Validation(
                    $"UnitPrice for product '{item.ProductId}' cannot be negative."));
            }
        }

        // ponytail: no inventory reservation here, so an order may be created for stock that
        // is not available. Call the reserve slice inside a transaction once orders must hold
        // stock at creation time.
        var order = Domain.Entities.Order.Create(
            command.CustomerId,
            command.Items.Select(item => new OrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            }));

        _dbContext.Orders.Add(order);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _cache.RemoveByTagAsync(CacheEntries.OrdersTag, cancellationToken);

        var response = new CreateOrderResponse(
            order.Id,
            order.TotalAmount,
            order.Status,
            order.CreatedAt);

        return Result.Success(response);
    }
}
