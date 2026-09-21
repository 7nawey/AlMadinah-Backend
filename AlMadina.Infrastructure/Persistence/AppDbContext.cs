using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AlMadina.Domain.Entities;

namespace AlMadina.Infrastructure.Persistence
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }
        public DbSet<ReturnRequest> ReturnRequests { get; set; }
        public DbSet<ReturnItem> ReturnItems { get; set; }
        public DbSet<Deal> Deals { get; set; }
        public DbSet<PaymentRecord> PaymentRecords { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>(entity =>
            {
                entity.Property(p => p.Price).HasPrecision(18, 2);
                entity.Property(p => p.CostPrice).HasPrecision(18, 2);
                entity.Property(p => p.DiscountPercentage).HasPrecision(5, 2);

                // Search columns must be bounded — SQL Server cannot
                // index nvarchar(max). 450 chars covers names and barcodes.
                entity.Property(p => p.NameAr).HasMaxLength(450);
                entity.Property(p => p.NameEn).HasMaxLength(450);
                entity.Property(p => p.Barcode).HasMaxLength(450);
                entity.Property(p => p.NameArNormalized).HasMaxLength(450);

                // Search indexes
                entity.HasIndex(p => p.NameArNormalized);
                entity.HasIndex(p => p.NameAr);
                entity.HasIndex(p => p.NameEn);
                entity.HasIndex(p => p.Barcode);
            });

            modelBuilder.Entity<Order>(entity =>
            {
                entity.Property(o => o.TotalPrice).HasPrecision(18, 2);
                entity.Property(o => o.DeliveryFee).HasPrecision(18, 2);

                entity.HasMany(o => o.Items)
                    .WithOne(i => i.Order)
                    .HasForeignKey(i => i.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.Property(i => i.UnitPrice).HasPrecision(18, 2);
                entity.Property(i => i.TotalPrice).HasPrecision(18, 2);
            });

            modelBuilder.Entity<StockMovement>(entity =>
            {
                entity.Property(s => s.UnitCost).HasPrecision(18, 2);
                entity.Property(s => s.TotalCost).HasPrecision(18, 2);
            });

            modelBuilder.Entity<Deal>(entity =>
            {
                entity.Property(d => d.OriginalPrice).HasPrecision(18, 2);
                entity.Property(d => d.DiscountedPrice).HasPrecision(18, 2);
                entity.Property(d => d.DiscountPercentage).HasPrecision(5, 2);
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasOne(p => p.Category)
                    .WithMany(c => c.Products)
                    .HasForeignKey(p => p.CategoryId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<ReturnRequest>(entity =>
            {
                entity.HasOne(r => r.Order)
                    .WithMany()
                    .HasForeignKey(r => r.OrderId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(r => r.Items)
                    .WithOne(i => i.ReturnRequest)
                    .HasForeignKey(i => i.ReturnRequestId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ReturnItem>(entity =>
            {
                entity.Property(i => i.UnitPrice).HasPrecision(18, 2);
            });
        }
    }
}
