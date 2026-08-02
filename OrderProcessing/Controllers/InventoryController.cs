using Microsoft.AspNetCore.Mvc;
using Order.Processing.Application.Abstractions.Messaging;
using Order.Processing.Application.Features.InventoryItems.GetInventoryAvailability;
using Order.Processing.Application.Features.InventoryItems.Update.ReleaseReservedItem;
using Order.Processing.Application.Features.InventoryItems.Update.ReserveInventoryItem;
using Order.Processing.Domain.Common;

namespace OrderProcessing.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class InventoryController : ControllerBase
{
    private readonly IQueryHandler<GetInventoryItemsAvailabilityQuery, Result<InventoryItemAvailabilityResponse>> _getAvailability;
    private readonly ICommandHandler<ReserveInventoryItemCommand> _reserveInventoryItem;
    private readonly ICommandHandler<ReleaseInventoryItemCommand> _releaseInventoryItem;

    public InventoryController(
        IQueryHandler<GetInventoryItemsAvailabilityQuery, Result<InventoryItemAvailabilityResponse>> getAvailability,
        ICommandHandler<ReserveInventoryItemCommand> reserveInventoryItem,
        ICommandHandler<ReleaseInventoryItemCommand> releaseInventoryItem)
    {
        _getAvailability = getAvailability;
        _reserveInventoryItem = reserveInventoryItem;
        _releaseInventoryItem = releaseInventoryItem;
    }

    [HttpGet("{productId}")]
    [ProducesResponseType<InventoryItemAvailabilityResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<Result<InventoryItemAvailabilityResponse>> GetAvailability(
        string productId,
        CancellationToken cancellationToken)
    {
        var response = await _getAvailability.HandleAsync(
            new GetInventoryItemsAvailabilityQuery(productId),
            cancellationToken);

        return response;
    }

    [HttpPost("reserve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<Result> Reserve(
        ReserveInventoryItemCommand command,
        CancellationToken cancellationToken)
    {
        var response = await _reserveInventoryItem.HandleAsync(command, cancellationToken);

        return response;
    }

    [HttpPost("release")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<Result> Release(
        ReleaseInventoryItemCommand command,
        CancellationToken cancellationToken)
    {
        var response = await _releaseInventoryItem.HandleAsync(command, cancellationToken);

        return response;
    }
}
