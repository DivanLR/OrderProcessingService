using Microsoft.EntityFrameworkCore;
using Order.Processing.Domain.Entities;

namespace Order.Processing.Application.Data;

public interface IApplicationDbContext
{
    DbSet<Domain.Entities.Order> Orders { get; }
    DbSet<InventoryItem> InventoryItems { get; }
    DbSet<PaymentTransaction> PaymentTransactions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
