using System;
using System.Collections.Generic;
using System.Text;

namespace Order.Processing.Domain.Entities;

public class InventoryItem
{
    public string ProductId { get; init; }
    public int AvailableQuantity { get; init; }
    public int ReservedQuantity { get; init; }
}
