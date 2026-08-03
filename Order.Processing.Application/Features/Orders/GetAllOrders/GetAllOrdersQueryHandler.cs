using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Order.Processing.Application.Abstractions.Caching;
using Order.Processing.Application.Abstractions.Messaging;
using Order.Processing.Application.Data;
using Order.Processing.Application.Features.Orders.GetOrder;
using Order.Processing.Domain.Common;

namespace Order.Processing.Application.Features.Orders.GetAllOrders;

public sealed class GetAllOrdersQueryHandler : IQueryHandler<GetAllOrdersQuery, Result<OrdersPageResponse>>
{
    private const int MaxPageSize = 50;

    private readonly IApplicationDbContext _context;
    private readonly HybridCache _cache;

    public GetAllOrdersQueryHandler(IApplicationDbContext context, HybridCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<Result<OrdersPageResponse>> HandleAsync(GetAllOrdersQuery query, CancellationToken cancellationToken = default)
    {
        int page = Math.Max(query.Page, 1);
        int pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        OrdersPageResponse response = await _cache.GetOrCreateAsync(
            $"orders:page:{page}:{pageSize}",
            (context: _context, page, pageSize),
            static async (state, token) =>
            {
                int totalCount = await state.context.Orders.CountAsync(token);

                var orders = await state.context.Orders
                    .AsNoTracking()
                    .OrderByDescending(o => o.CreatedAt)
                    .ThenBy(o => o.Id)
                    .Skip((state.page - 1) * state.pageSize)
                    .Take(state.pageSize)
                    .ToListAsync(token);

                OrderResponse[] items =
                [
                    .. orders.Select(o => new OrderResponse(
                        o.Id,
                        o.CustomerId,
                        [.. o.Items.Select(item => new OrderItemResponse(item.ProductId, item.Quantity, item.UnitPrice))],
                        o.TotalAmount,
                        o.Status,
                        o.CreatedAt,
                        o.UpdatedAt))
                ];

                return new OrdersPageResponse(items, state.page, state.pageSize, totalCount);
            },
            CacheEntries.Options,
            tags: [CacheEntries.OrdersTag],
            cancellationToken: cancellationToken);

        return Result.Success(response);
    }
}
