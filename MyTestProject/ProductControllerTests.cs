/*
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartInventoryManagement.Database;
using SmartInventoryManagement.Controllers;
using SmartInventoryManagement.Models;
using SmartInventoryManagement.Services;
using Moq;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace MyTestProject
{
    public class ProductControllerTests
    {
        private ApplicationDbContext GetTestDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("ProductControllerTestDb" + System.Guid.NewGuid().ToString())
                .Options;
            
            var context = new ApplicationDbContext(options);
            
            // Seed with test data
            var category = new Category { Id = 1, Name = "Test Category" };
            context.Categories.Add(category);
            
            context.Products.AddRange(
                new Product { Id = 1, Name = "Product 1", Price = 10.99m, StockQuantity = 20, CategoryId = 1 },
                new Product { Id = 2, Name = "Product 2", Price = 5.99m, StockQuantity = 5, CategoryId = 1 },
                new Product { Id = 3, Name = "Product 3", Price = 15.99m, StockQuantity = 0, CategoryId = 1 }
            );
            
            context.SaveChanges();
            return context;
        }
        
        [Fact]
        public async Task Index_ReturnsAllProducts_WhenNoFiltersApplied()
        {
            // Arrange
            using var context = GetTestDbContext();
            var mockLogger = new Mock<ILogger<ProductsController>>();
            var mockEmailService = new Mock<IEmailService>();
            
            var controller = new ProductsController(context, mockEmailService.Object, mockLogger.Object);
            
            // Act
            var result = await controller.Index(null, null, null, null, null);
            
            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Product>>(viewResult.ViewData.Model);
            Assert.Equal(3, model.Count());
        }
        
        [Fact]
        public async Task Index_ReturnsFilteredProducts_WhenSearchStringProvided()
        {
            // Arrange
            using var context = GetTestDbContext();
            var mockLogger = new Mock<ILogger<ProductsController>>();
            var mockEmailService = new Mock<IEmailService>();
            
            var controller = new ProductsController(context, mockEmailService.Object, mockLogger.Object);
            
            // Act
            var result = await controller.Index("Product 1", null, null, null, null);
            
            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Product>>(viewResult.ViewData.Model);
            Assert.Single(model);
            Assert.Equal("Product 1", model.First().Name);
        }
        
        [Fact]
        public async Task Index_ReturnsOutOfStockProducts_WhenFilterApplied()
        {
            // Arrange
            using var context = GetTestDbContext();
            var mockLogger = new Mock<ILogger<ProductsController>>();
            var mockEmailService = new Mock<IEmailService>();
            
            var controller = new ProductsController(context, mockEmailService.Object, mockLogger.Object);
            
            // Act
            var result = await controller.Index(null, null, true, null, null);
            
            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Product>>(viewResult.ViewData.Model);
            Assert.Single(model);
            Assert.Equal(0, model.First().StockQuantity);
        }
        
        [Fact]
        public async Task Details_ReturnsNotFound_WhenIdIsNull()
        {
            // Arrange
            using var context = GetTestDbContext();
            var mockLogger = new Mock<ILogger<ProductsController>>();
            var mockEmailService = new Mock<IEmailService>();
            
            var controller = new ProductsController(context, mockEmailService.Object, mockLogger.Object);
            
            // Act
            var result = await controller.Details(null);
            
            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
        
        [Fact]
        public async Task Details_ReturnsNotFound_WhenProductDoesNotExist()
        {
            // Arrange
            using var context = GetTestDbContext();
            var mockLogger = new Mock<ILogger<ProductsController>>();
            var mockEmailService = new Mock<IEmailService>();
            
            var controller = new ProductsController(context, mockEmailService.Object, mockLogger.Object);
            
            // Act
            var result = await controller.Details(999);
            
            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
        
        [Fact]
        public async Task Details_ReturnsViewWithProduct_WhenProductExists()
        {
            // Arrange
            using var context = GetTestDbContext();
            var mockLogger = new Mock<ILogger<ProductsController>>();
            var mockEmailService = new Mock<IEmailService>();
            
            var controller = new ProductsController(context, mockEmailService.Object, mockLogger.Object);
            
            // Act
            var result = await controller.Details(1);
            
            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Product>(viewResult.Model);
            Assert.Equal(1, model.Id);
            Assert.Equal("Product 1", model.Name);
        }
    }
}
*/ 