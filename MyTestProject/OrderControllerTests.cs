/*
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartInventoryManagement.Database;
using SmartInventoryManagement.Controllers;
using SmartInventoryManagement.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using System;

namespace MyTestProject
{
    public class OrderControllerTests
    {
        private ApplicationDbContext GetTestDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("OrderControllerTestDb" + System.Guid.NewGuid().ToString())
                .Options;
            
            var context = new ApplicationDbContext(options);
            
            // Add test categories
            var electronics = new Category { Id = 1, Name = "Electronics", Description = "Electronic devices" };
            var furniture = new Category { Id = 2, Name = "Furniture", Description = "Home furniture" };
            
            context.Categories.AddRange(electronics, furniture);
            
            // Add test items
            var laptop = new Item { Id = 1, Name = "Laptop", Description = "High-performance laptop", Price = 1200.00M, Quantity = 10, CategoryId = 1, Category = electronics };
            var smartphone = new Item { Id = 2, Name = "Smartphone", Description = "Latest smartphone model", Price = 800.00M, Quantity = 20, CategoryId = 1, Category = electronics };
            var chair = new Item { Id = 3, Name = "Office Chair", Description = "Ergonomic office chair", Price = 300.00M, Quantity = 5, CategoryId = 2, Category = furniture };
            
            context.Items.AddRange(laptop, smartphone, chair);
            
            // Add test orders
            var testUserOrder = new Order 
            { 
                Id = 1, 
                UserId = "testuser", 
                OrderDate = DateTime.Now.AddDays(-5),
                TotalAmount = 2300.00M
            };
            
            var adminUserOrder = new Order 
            { 
                Id = 2, 
                UserId = "adminuser", 
                OrderDate = DateTime.Now.AddDays(-2),
                TotalAmount = 800.00M
            };
            
            context.Orders.AddRange(testUserOrder, adminUserOrder);
            
            // Add order items
            context.OrderItems.AddRange(
                new OrderItem { Id = 1, OrderId = 1, ItemId = 1, Quantity = 1, UnitPrice = 1200.00M, Item = laptop },
                new OrderItem { Id = 2, OrderId = 1, ItemId = 3, Quantity = 3, UnitPrice = 300.00M, Item = chair },
                new OrderItem { Id = 3, OrderId = 2, ItemId = 2, Quantity = 1, UnitPrice = 800.00M, Item = smartphone }
            );
            
            context.SaveChanges();
            return context;
        }

        private OrdersController SetupControllerWithUser(ApplicationDbContext context, string userId = "testuser", bool isAdmin = false)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, "Test User")
            };
            
            if (isAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }
            
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "mock"));

            var controller = new OrdersController(context);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            
            controller.TempData = new TempDataDictionary(
                new DefaultHttpContext(), 
                Mock.Of<ITempDataProvider>());

            return controller;
        }
        
        [Fact]
        public async Task Index_ReturnsUserOrders_ForRegularUser()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUser(context, "testuser");
            
            // Act
            var result = await controller.Index();
            
            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Order>>(viewResult.Model);
            Assert.Single(model); // Only testuser's order
            Assert.Equal("testuser", model.First().UserId);
        }
        
        [Fact]
        public async Task Index_ReturnsAllOrders_ForAdminUser()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUser(context, "adminuser", isAdmin: true);
            
            // Act
            var result = await controller.Index();
            
            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Order>>(viewResult.Model);
            Assert.Equal(2, model.Count()); // All orders
        }
        
        [Fact]
        public async Task Details_ReturnsNotFound_WhenIdIsNull()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUser(context);
            
            // Act
            var result = await controller.Details(null);
            
            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
        
        [Fact]
        public async Task Details_ReturnsNotFound_WhenOrderDoesNotExist()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUser(context);
            
            // Act
            var result = await controller.Details(999);
            
            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
        
        [Fact]
        public async Task Details_ReturnsNotFound_WhenOrderBelongsToAnotherUser()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUser(context, "otheruser"); // Not the owner of any order
            
            // Act
            var result = await controller.Details(1); // Order belongs to testuser
            
            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
        
        [Fact]
        public async Task Details_ReturnsViewResult_WhenUserOwnsOrder()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUser(context, "testuser");
            
            // Act
            var result = await controller.Details(1); // Order belongs to testuser
            
            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Order>(viewResult.Model);
            Assert.Equal(1, model.Id);
            Assert.Equal("testuser", model.UserId);
            
            // Verify order items are included
            Assert.NotNull(model.OrderItems);
            Assert.Equal(2, model.OrderItems.Count);
        }
        
        [Fact]
        public async Task Details_ReturnsViewResult_ForAnyOrder_WhenUserIsAdmin()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUser(context, "adminuser", isAdmin: true);
            
            // Act
            var result = await controller.Details(1); // Order belongs to testuser, not adminuser
            
            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Order>(viewResult.Model);
            Assert.Equal(1, model.Id);
            Assert.Equal("testuser", model.UserId);
        }
        
        [Fact]
        public async Task MarkAsShipped_ReturnsUnauthorized_WhenUserIsNotAdmin()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUser(context, "testuser");
            
            // Act
            var result = await controller.MarkAsShipped(1);
            
            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }
        
        [Fact]
        public async Task MarkAsShipped_ReturnsNotFound_WhenOrderDoesNotExist()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUser(context, "adminuser", isAdmin: true);
            
            // Act
            var result = await controller.MarkAsShipped(999);
            
            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
        
        [Fact]
        public async Task MarkAsShipped_UpdatesOrderStatus_AndRedirectsToIndex()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUser(context, "adminuser", isAdmin: true);
            
            // Act
            var result = await controller.MarkAsShipped(1);
            
            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            
            // Verify order status was updated
            var order = await context.Orders.FindAsync(1);
            Assert.True(order.IsShipped);
            Assert.NotNull(order.ShippedDate);
        }
        
        [Fact]
        public async Task GenerateReport_ReturnsUnauthorized_WhenUserIsNotAdmin()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUser(context, "testuser");
            
            // Act
            var result = controller.GenerateReport();
            
            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }
        
        [Fact]
        public void GenerateReport_ReturnsViewResult_WithReportData()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUser(context, "adminuser", isAdmin: true);
            
            // Act
            var result = controller.GenerateReport();
            
            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            
            // Verify report data is included in ViewBag
            Assert.NotNull(viewResult.ViewBag.TotalOrders);
            Assert.NotNull(viewResult.ViewBag.TotalRevenue);
            Assert.NotNull(viewResult.ViewBag.TopSellingItems);
        }
    }
}
*/ 