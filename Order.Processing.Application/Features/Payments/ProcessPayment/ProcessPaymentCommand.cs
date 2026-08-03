using Order.Processing.Application.Abstractions.Messaging;
using Order.Processing.Domain.Common;
using Order.Processing.Domain.Entities;

namespace Order.Processing.Application.Features.Payments.ProcessPayment;

public sealed record ProcessPaymentCommand(string OrderId, decimal Amount)
    : ICommand<Result<ProcessPaymentResponse>>;

public sealed record ProcessPaymentResponse(Guid TransactionId, PaymentStatus Status, DateTime ProcessedAt);
