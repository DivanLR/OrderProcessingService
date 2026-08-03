using Order.Processing.Application.Abstractions.Messaging;
using Order.Processing.Application.Features.Orders.GetOrder;
using Order.Processing.Domain.Common;

namespace Order.Processing.Application.Features.Orders.GetAllOrders;

public sealed record GetAllOrdersQuery(int Page, int PageSize) : IQuery<Result<OrdersPageResponse>>;

public sealed record OrdersPageResponse(
    IReadOnlyCollection<OrderResponse> Orders,
    int Page,
    int PageSize,
    int TotalCount);
