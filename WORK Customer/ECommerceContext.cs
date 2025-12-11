using Microsoft.EntityFrameworkCore;

namespace WORK_Customer
{
    public class ECommerceContext : DbContext
    {
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderRow> OrderRows => Set<OrderRow>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var dbPath = Path.Combine(AppContext.BaseDirectory, "ecommerce.db");
            optionsBuilder.UseSqlite($"Filename={dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>(e =>
            {
                e.HasKey(x => x.CategoryId);
                e.Property(x => x.Name).IsRequired().HasMaxLength(200);
                e.HasOne(c => c.Parent)
                 .WithMany(c => c.Categories)
                 .HasForeignKey(c => c.CategoryId1)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Product>(e =>
            {
                e.HasKey(p => p.id);
                e.Property(p => p.name).IsRequired().HasMaxLength(200);
                e.Property(p => p.price).IsRequired();
                e.HasOne(p => p.Category)
                 .WithMany(c => c.Products)
                 .HasForeignKey(p => p.CategoryID)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Customer>(e =>
            {
                e.HasKey(c => c.CustomerId);
                e.Property(c => c.CustomerName).IsRequired();
                e.Property(c => c.LastName).IsRequired();
                e.Property(c => c.Email).IsRequired();
                // Map CLR property Password to existing DB column EncryptedSsn (legacy)
                e.Property(c => c.Password).HasColumnName("EncryptedSsn").IsRequired();
            });

            modelBuilder.Entity<Order>(e =>
            {
                e.HasKey(o => o.OrderId);
                e.Property(o => o.CreatedAt).IsRequired();
                e.HasOne(o => o.Customer)
                 .WithMany()
                 .HasForeignKey(o => o.CustomerId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<OrderRow>(e =>
            {
                e.HasKey(r => r.OrderRowId);
                e.Property(r => r.UnitPrice).IsRequired();
                e.HasOne(r => r.Order)
                 .WithMany(o => o.Rows)
                 .HasForeignKey(r => r.OrderId)
                 .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(r => r.Product)
                 .WithMany()
                 .HasForeignKey(r => r.ProductId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Order>().HasIndex(o => o.CreatedAt);
            modelBuilder.Entity<Order>().HasIndex(o => o.CustomerId);
        }
    }
}