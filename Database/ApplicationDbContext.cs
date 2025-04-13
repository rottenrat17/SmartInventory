using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using SmartInventoryManagement.Models;
using Microsoft.Extensions.Logging;

namespace SmartInventoryManagement.Database
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        private readonly ILogger<ApplicationDbContext> _logger;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            ILogger<ApplicationDbContext> logger = null) : base(options)
        {
            _logger = logger;
            _logger?.LogInformation("ApplicationDbContext initialized with connection: {Connection}", 
                Database.GetConnectionString());
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId);

            // Seed initial data for Categories
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Electronics", Description = "Electronic devices and gadgets" },
                new Category { Id = 2, Name = "Clothing", Description = "Apparel and fashion items" },
                new Category { Id = 3, Name = "Food", Description = "Grocery and food items" }
            );

            // Seed initial data for Products
            modelBuilder.Entity<Product>().HasData(
                new Product 
                { 
                    Id = 1, 
                    Name = "Laptop", 
                    CategoryId = 1, 
                    Price = 999.99m, 
                    StockQuantity = 15, 
                    LowStockThreshold = 5 
                },
                new Product 
                { 
                    Id = 2, 
                    Name = "T-Shirt", 
                    CategoryId = 2, 
                    Price = 19.99m, 
                    StockQuantity = 100, 
                    LowStockThreshold = 20 
                },
                new Product 
                { 
                    Id = 3, 
                    Name = "Coffee", 
                    CategoryId = 3, 
                    Price = 8.99m, 
                    StockQuantity = 50, 
                    LowStockThreshold = 10 
                }
            );
        }
    }
}
