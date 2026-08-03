using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Order.Processing.Application.Abstractions.Caching;
using Order.Processing.Application.Abstractions.Messaging;
using Order.Processing.Application.Data;
using Order.Processing.Domain.Common;

namespace Order.Processing.Application.Features.Orders.GetOrder;

public sealed class GetOrderQueryHandler : IQueryHandler<GetOrderQuery, Result<OrderResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly HybridCache _cache;

    public GetOrderQueryHandler(IApplicationDbContext context, HybridCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<Result<OrderResponse>> HandleAsync(GetOrderQuery query, CancellationToken cancellationToken = default)
    {
        // ponytail: a miss is cached as well, because HybridCache always stores the factory
        // result. Every write invalidates the orders tag, so a create or status change clears
        // the negative entry before it can be read again.
        OrderResponse? response = await _cache.GetOrCreateAsync(
            $"orders:{query.OrderId}",
            (context: _context, query.OrderId),
            static async (state, token) =>
            {
                var order = await state.context.Orders
                    .AsNoTracking()
                    .Where(o => o.Id == state.OrderId)
                    .FirstOrDefaultAsync(token);

                return order is null
                    ? null
                    : new OrderResponse(
                        order.Id,
                        order.CustomerId,
                        [.. order.Items.Select(item => new OrderItemResponse(item.ProductId, item.Quantity, item.UnitPrice))],
                        order.TotalAmount,
                        order.Status,
                        order.CreatedAt,
                        order.UpdatedAt);
            },
            CacheEntries.Options,
            tags: [CacheEntries.OrdersTag],
            cancellationToken: cancellationToken);

        if (response is null)
        {
            return Result.Failure<OrderResponse>(Error.NotFound(
                "order.not_found",
                $"Order with Id {query.OrderId} not found."));
        }

        return Result.Success(response);
    }
}
