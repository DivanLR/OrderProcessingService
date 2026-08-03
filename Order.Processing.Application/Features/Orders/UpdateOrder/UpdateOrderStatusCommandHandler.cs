using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Order.Processing.Application.Abstractions.Caching;
using Order.Processing.Application.Abstractions.Messaging;
using Order.Processing.Application.Data;
using Order.Processing.Domain.Common;
using Order.Processing.Domain.Entities;

namespace Order.Processing.Application.Features.Orders.UpdateOrder;

public sealed class UpdateOrderStatusCommandHandler : ICommandHandler<UpdateOrderStatusCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly HybridCache _cache;

    public UpdateOrderStatusCommandHandler(IApplicationDbContext dbContext, HybridCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task<Result> HandleAsync(UpdateOrderStatusCommand command, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse(command.Status, ignoreCase: true, out Status status) || !Enum.IsDefined(status))
        {
            return Result.Failure(Error.Validation(
                $"'{command.Status}' is not a valid order status. Valid values: {string.Join(", ", Enum.GetNames<Status>())}."));
        }

        var order = await _dbContext.Orders
            .Where(o => o.Id == command.OrderId)
            .FirstOrDefaultAsync(cancellationToken);

        if (order is null)
        {
            return Result.Failure(Error.NotFound(
                "order.not_found",
                $"Order with Id '{command.OrderId}' not found."));
        }

        var update = order.UpdateStatus(status);

        if (update.IsFailure)
        {
            return update;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _cache.RemoveByTagAsync(CacheEntries.OrdersTag, cancellationToken);

        return Result.Success();
    }
}
