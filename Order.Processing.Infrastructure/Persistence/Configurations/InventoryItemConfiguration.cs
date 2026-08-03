using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Order.Processing.Domain.Entities;

namespace Order.Processing.Infrastructure.Persistence.Configurations;

public sealed class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(inventoryItem => inventoryItem.ProductId);

        builder.Property(inventoryItem => inventoryItem.ProductId)
            .IsRequired()
            .HasMaxLength(64);
    }
}
