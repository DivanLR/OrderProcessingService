using System;
using System.Collections.Generic;
using System.Text;
using Order.Processing.Application.Abstractions.Messaging;

namespace Order.Processing.Application.Features.InventoryItems.Update;

public sealed record UpdateInventoryItemCommand(string ProductId, int Quantity) : ICommand;
