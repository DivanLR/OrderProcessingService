using Microsoft.AspNetCore.Mvc;
using Order.Processing.Application.Abstractions.Messaging;
using Order.Processing.Application.Features.Payments.GetPaymentStatus;
using Order.Processing.Application.Features.Payments.ProcessPayment;
using Order.Processing.Domain.Common;

namespace OrderProcessing.Controllers;

[ApiController]
[Route("api/payments")]
public sealed class PaymentController : ControllerBase
{
    private readonly ICommandHandler<ProcessPaymentCommand, Result<ProcessPaymentResponse>> _processPayment;
    private readonly IQueryHandler<GetPaymentStatusQuery, Result<PaymentStatusResponse>> _getPaymentStatus;

    public PaymentController(
        ICommandHandler<ProcessPaymentCommand, Result<ProcessPaymentResponse>> processPayment,
        IQueryHandler<GetPaymentStatusQuery, Result<PaymentStatusResponse>> getPaymentStatus)
    {
        _processPayment = processPayment;
        _getPaymentStatus = getPaymentStatus;
    }

    [HttpPost("process")]
    [ProducesResponseType<ProcessPaymentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<Result<ProcessPaymentResponse>> Process(
        ProcessPaymentCommand command,
        CancellationToken cancellationToken)
    {
        var response = await _processPayment.HandleAsync(command, cancellationToken);

        return response;
    }

    [HttpGet("{transactionId:guid}")]
    [ProducesResponseType<PaymentStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<Result<PaymentStatusResponse>> GetStatus(
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        var response = await _getPaymentStatus.HandleAsync(
            new GetPaymentStatusQuery(transactionId),
            cancellationToken);

        return response;
    }
}
