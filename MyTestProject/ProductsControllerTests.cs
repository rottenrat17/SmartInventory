using Xunit;
using Moq;
using SmartInventoryManagement.Controllers;
using SmartInventoryManagement.Database;
using SmartInventoryManagement.Models;
using SmartInventoryManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyTestProject
{
    public class ProductsControllerTests
    {
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<ILogger<ProductsController>> _mockLogger;
        private readonly DbContextOptions<ApplicationDbContext> _contextOptions;

        public ProductsControllerTests()
        {
            _mockEmailService = new Mock<IEmailService>();
            _mockLogger = new Mock<ILogger<ProductsController>>();
            
            // Set up in-memory database for testing
            _contextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("TestProductDb")
                .Options;
                
            // Seed the database
            using (var context = new ApplicationDbContext(_contextOptions))
            {
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();
                
                // Add test categories
                var category = new Category { Id = 1, Name = "Test Category" };
                context.Categories.Add(category);
                
                // Add test products
                context.Products.Add(new Product 
                { 
                    Id = 1, 
                    Name = "Test Product 1", 
                    CategoryId = 1, 
                    Price = 9.99m, 
                    StockQuantity = 20 
                });
                context.Products.Add(new Product 
                { 
                    Id = 2, 
                    Name = "Test Product 2", 
                    CategoryId = 1, 
                    Price = 19.99m, 
                    StockQuantity = 5 
                });
                
                context.SaveChanges();
            }
        }

        [Fact]
        public async Task Index_ReturnsAllProducts_WhenNoFilterProvided()
        {
            // Arrange
            using (var context = new ApplicationDbContext(_contextOptions))
            {
                var controller = new ProductsController(context, _mockEmailService.Object, _mockLogger.Object);
                
                // Act
                var result = await controller.Index(null, null, null, null, null);
                
                // Assert
                var viewResult = Assert.IsType<ViewResult>(result);
                var model = Assert.IsAssignableFrom<IEnumerable<Product>>(viewResult.Model);
                Assert.Equal(2, model.Count());
            }
        }

        [Fact]
        public async Task Index_ReturnsFilteredProducts_WhenNameFilterProvided()
        {
            // Arrange
            using (var context = new ApplicationDbContext(_contextOptions))
            {
                var controller = new ProductsController(context, _mockEmailService.Object, _mockLogger.Object);
                
                // Act
                var result = await controller.Index("Test Product 1", null, null, null, null);
                
                // Assert
                var viewResult = Assert.IsType<ViewResult>(result);
                var model = Assert.IsAssignableFrom<IEnumerable<Product>>(viewResult.Model);
                Assert.Single(model);
                Assert.Equal("Test Product 1", model.First().Name);
            }
        }

        [Fact]
        public async Task SearchProducts_ReturnsJson_WithFilteredProducts()
        {
            // Arrange
            using (var context = new ApplicationDbContext(_contextOptions))
            {
                var controller = new ProductsController(context, _mockEmailService.Object, _mockLogger.Object);
                
                // Act
                var result = await controller.SearchProducts("Test Product 1", null, null, null);
                
                // Assert
                var jsonResult = Assert.IsType<JsonResult>(result);
                var model = Assert.IsAssignableFrom<IEnumerable<object>>(jsonResult.Value);
                Assert.Single(model);
            }
        }
    }
} 