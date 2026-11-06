using ArchivePieces.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ArchivePieces.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductImage> ProductImages => Set<ProductImage>();
        public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Category
            builder.Entity<Category>(e =>
            {
                e.HasIndex(c => c.Slug).IsUnique();
                e.Property(c => c.Name).IsRequired().HasMaxLength(100);
            });

            // Product
            builder.Entity<Product>(e =>
            {
                e.HasIndex(p => p.Slug).IsUnique();
                e.HasOne(p => p.Category)
                    .WithMany(c => c.Products)
                    .HasForeignKey(p => p.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ProductImage
            builder.Entity<ProductImage>(e =>
            {
                e.HasOne(i => i.Product)
                    .WithMany(p => p.Images)
                    .HasForeignKey(i => i.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ProductVariant
            builder.Entity<ProductVariant>(e =>
            {
                e.HasOne(v => v.Product)
                    .WithMany(p => p.Variants)
                    .HasForeignKey(v => v.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Order
            builder.Entity<Order>(e =>
            {
                e.HasIndex(o => o.OrderNumber).IsUnique();
                e.HasOne(o => o.User)
                    .WithMany(u => u.Orders)
                    .HasForeignKey(o => o.UserId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // OrderItem — no cascade from Product (product can be deleted later)
            builder.Entity<OrderItem>(e =>
            {
                e.HasOne(i => i.Order)
                    .WithMany(o => o.Items)
                    .HasForeignKey(i => i.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(i => i.Product)
                    .WithMany()
                    .HasForeignKey(i => i.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                // LineTotal is computed, ignore in DB
                e.Ignore(i => i.LineTotal);
            });

            // OrderStatusHistory
            builder.Entity<OrderStatusHistory>(e =>
            {
                e.HasOne(h => h.Order)
                    .WithMany(o => o.StatusHistory)
                    .HasForeignKey(h => h.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Ignore computed properties
            builder.Entity<Product>()
                .Ignore(p => p.IsNew)
                .Ignore(p => p.IsSoldOut)
                .Ignore(p => p.TotalStock)
                .Ignore(p => p.PrimaryImagePath);

            builder.Entity<ProductVariant>()
                .Ignore(v => v.IsLowStock)
                .Ignore(v => v.IsOutOfStock);
        }
    }
}
