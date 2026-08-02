using System;
using System.Collections.Generic;
using System.Text;
using Order.Processing.Application.Abstractions.Messaging;
using Order.Processing.Application.Data;
using Order.Processing.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Order.Processing.Application.Features.InventoryItems.Update.ReserveInventoryItem;

public sealed class ReserveInventoryItemCommandHandler()
{
    private readonly IApplicationDbContext _dbContext;

    public ReserveInventoryItemCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> HandleAsync(UpdateInventoryItemCommand command, CancellationToken cancellationToken = default)
    {
        var inventoryItem = await _dbContext.InventoryItems.Where(ii => ii.ProductId == command.ProductId).FirstOrDefaultAsync(cancellationToken);
        if (inventoryItem == null)
        {
            return Result.Failure(new Error($"Inventory item with ProductId '{command.ProductId}' not found."));
        }

        inventoryItem!.Reserve(command.Quantity);

        return Result.Success();
    }
}
