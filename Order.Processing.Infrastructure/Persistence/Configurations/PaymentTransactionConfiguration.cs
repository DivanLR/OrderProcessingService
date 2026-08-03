using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Order.Processing.Domain.Entities;

namespace Order.Processing.Infrastructure.Persistence.Configurations;

public sealed class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(paymentTransaction => paymentTransaction.TransactionId);

        builder.Property(paymentTransaction => paymentTransaction.OrderId)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(paymentTransaction => paymentTransaction.Amount)
            .HasPrecision(18, 2);

        builder.HasIndex(paymentTransaction => paymentTransaction.OrderId);
    }
}
