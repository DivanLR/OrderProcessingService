using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Order.Processing.Application.Abstractions.Caching;
using Order.Processing.Application.Abstractions.Messaging;
using Order.Processing.Application.Data;
using Order.Processing.Domain.Common;
using Order.Processing.Domain.Entities;

namespace Order.Processing.Application.Features.Payments.ProcessPayment;

public sealed class ProcessPaymentCommandHandler
    : ICommandHandler<ProcessPaymentCommand, Result<ProcessPaymentResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly HybridCache _cache;

    public ProcessPaymentCommandHandler(IApplicationDbContext dbContext, HybridCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task<Result<ProcessPaymentResponse>> HandleAsync(ProcessPaymentCommand command, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(command.OrderId, out Guid orderId))
        {
            return Result.Failure<ProcessPaymentResponse>(Error.Validation(
                $"'{command.OrderId}' is not a valid order id."));
        }

        if (command.Amount <= 0)
        {
            return Result.Failure<ProcessPaymentResponse>(Error.Validation("Amount must be greater than zero."));
        }

        bool orderExists = await _dbContext.Orders
            .AnyAsync(o => o.Id == orderId, cancellationToken);

        if (!orderExists)
        {
            return Result.Failure<ProcessPaymentResponse>(Error.NotFound(
                "order.not_found",
                $"Order with Id '{command.OrderId}' not found."));
        }

        // ponytail: no payment gateway wired, so the transaction is recorded as Completed on
        // creation. Add a gateway call here and set Status from its outcome (Pending while
        // in flight, Failed on decline) when a real provider is integrated.
        var transaction = new PaymentTransaction
        {
            OrderId = command.OrderId,
            Amount = command.Amount,
            Status = PaymentStatus.Completed,
            ProcessedAt = DateTime.UtcNow
        };

        _dbContext.PaymentTransactions.Add(transaction);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _cache.RemoveByTagAsync(CacheEntries.PaymentsTag, cancellationToken);

        var response = new ProcessPaymentResponse(
            transaction.TransactionId,
            transaction.Status,
            transaction.ProcessedAt);

        return Result.Success(response);
    }
}
