using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Order.Processing.Application.Abstractions.Caching;
using Order.Processing.Application.Abstractions.Messaging;
using Order.Processing.Application.Data;
using Order.Processing.Domain.Common;

namespace Order.Processing.Application.Features.Payments.GetPaymentStatus;

public sealed class GetPaymentStatusQueryHandler
    : IQueryHandler<GetPaymentStatusQuery, Result<PaymentStatusResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly HybridCache _cache;

    public GetPaymentStatusQueryHandler(IApplicationDbContext context, HybridCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<Result<PaymentStatusResponse>> HandleAsync(GetPaymentStatusQuery query, CancellationToken cancellationToken = default)
    {
        PaymentStatusResponse? response = await _cache.GetOrCreateAsync(
            $"payments:{query.TransactionId}",
            (context: _context, query.TransactionId),
            static async (state, token) =>
            {
                var transaction = await state.context.PaymentTransactions
                    .AsNoTracking()
                    .Where(pt => pt.TransactionId == state.TransactionId)
                    .FirstOrDefaultAsync(token);

                return transaction is null
                    ? null
                    : new PaymentStatusResponse(
                        transaction.TransactionId,
                        transaction.OrderId,
                        transaction.Amount,
                        transaction.Status,
                        transaction.ProcessedAt);
            },
            CacheEntries.Options,
            tags: [CacheEntries.PaymentsTag],
            cancellationToken: cancellationToken);

        if (response is null)
        {
            return Result.Failure<PaymentStatusResponse>(Error.NotFound(
                "payment.not_found",
                $"Payment transaction with TransactionId {query.TransactionId} not found."));
        }

        return Result.Success(response);
    }
}
