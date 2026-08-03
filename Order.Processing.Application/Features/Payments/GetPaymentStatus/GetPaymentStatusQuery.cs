using Order.Processing.Application.Abstractions.Messaging;
using Order.Processing.Domain.Common;
using Order.Processing.Domain.Entities;

namespace Order.Processing.Application.Features.Payments.GetPaymentStatus;

public sealed record GetPaymentStatusQuery(Guid TransactionId) : IQuery<Result<PaymentStatusResponse>>;

public sealed record PaymentStatusResponse(
    Guid TransactionId,
    string OrderId,
    decimal Amount,
    PaymentStatus Status,
    DateTime ProcessedAt);
