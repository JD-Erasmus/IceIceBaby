using IceIceBaby.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IceIceBaby.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products => Set<Product>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<DeliveryRun> DeliveryRuns => Set<DeliveryRun>();
        public DbSet<DeliveryStop> DeliveryStops => Set<DeliveryStop>();
        public DbSet<Driver> Drivers => Set<Driver>();
        public DbSet<Vehicle> Vehicles => Set<Vehicle>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Product>().HasIndex(p => p.Sku).IsUnique();

            builder.Entity<Order>()
                .HasIndex(o => o.OrderNo)
                .IsUnique();

            builder.Entity<DeliveryStop>()
                .HasIndex(ds => ds.OrderId)
                .IsUnique();

            builder.Entity<DeliveryStop>()
                .HasIndex(ds => new { ds.DeliveryRunId, ds.Seq })
                .IsUnique();

            builder.Entity<OrderItem>()
                .Property(p => p.UnitPriceSnapshot)
                .HasColumnType("decimal(18,2)");

            builder.Entity<OrderItem>()
                .Property(p => p.LineTotal)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasColumnType("decimal(18,2)");

            // Store DeliveryRunStatus as string for readability
            builder.Entity<DeliveryRun>()
                .Property(r => r.Status)
                .HasConversion<string>()
                .HasMaxLength(30);
        }
    }
}
