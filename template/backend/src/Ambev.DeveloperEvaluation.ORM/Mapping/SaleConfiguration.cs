using Ambev.DeveloperEvaluation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ambev.DeveloperEvaluation.ORM.Mapping;

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("Sales");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(s => s.SaleNumber).IsRequired().HasMaxLength(100);
        builder.Property(s => s.SaleDate).IsRequired();
        builder.Property(s => s.CustomerId).HasColumnType("uuid").IsRequired();
        builder.Property(s => s.CustomerName).IsRequired().HasMaxLength(200);
        builder.Property(s => s.BranchId).HasColumnType("uuid").IsRequired();
        builder.Property(s => s.BranchName).IsRequired().HasMaxLength(200);

        builder.Property(s => s.TotalAmount).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(s => s.IsCancelled).IsRequired();

        // Configure relationship and backing field access
        builder.HasMany<SaleItem>("_items")
               .WithOne()
               .HasForeignKey(nameof(SaleItem.SaleId))
               .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.Items)
               .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}