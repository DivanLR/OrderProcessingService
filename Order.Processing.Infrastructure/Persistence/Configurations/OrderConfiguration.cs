using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Order.Processing.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Domain.Entities.Order>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Order> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(order => order.Id);

        builder.Property(order => order.CustomerId)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(order => order.TotalAmount)
            .HasPrecision(18, 2);

        builder.HasIndex(order => order.CustomerId);

        builder.OwnsMany(order => order.Items, itemBuilder =>
        {
            itemBuilder.Property(item => item.ProductId)
                .IsRequired()
                .HasMaxLength(64);

            itemBuilder.Property(item => item.UnitPrice)
                .HasPrecision(18, 2);
        });
    }
}
