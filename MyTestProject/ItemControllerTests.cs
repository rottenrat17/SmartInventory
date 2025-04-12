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
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace MyTestProject
{
    public class ItemControllerTests
    {
        private ApplicationDbContext GetTestDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("ItemControllerTestDb" + System.Guid.NewGuid().ToString())
                .Options;
            
            var context = new ApplicationDbContext(options);
            
            // Add test categories
            var electronics = new Category { Id = 1, Name = "Electronics", Description = "Electronic devices" };
            var furniture = new Category { Id = 2, Name = "Furniture", Description = "Home furniture" };
            
            context.Categories.AddRange(electronics, furniture);
            
            // Add test items
            context.Items.AddRange(
                new Item { Id = 1, Name = "Laptop", Description = "High-performance laptop", Price = 1200.00M, Quantity = 10, CategoryId = 1, Category = electronics },
                new Item { Id = 2, Name = "Smartphone", Description = "Latest smartphone model", Price = 800.00M, Quantity = 20, CategoryId = 1, Category = electronics },
                new Item { Id = 3, Name = "Office Chair", Description = "Ergonomic office chair", Price = 300.00M, Quantity = 5, CategoryId = 2, Category = furniture }
            );
            
            context.SaveChanges();
            return context;
        }

        private ItemController SetupControllerWithUser(ApplicationDbContext context, bool isAdmin = false)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testuser"),
                new Claim(ClaimTypes.Name, "Test User")
            };
            
            if (isAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }
            
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "mock"));

            // Mock web host environment
            var mockEnvironment = new Mock<IWebHostEnvironment>();
            mockEnvironment.Setup(m => m.WebRootPath).Returns(Path.GetTempPath());
            
            var controller = new ItemController(context, mockEnvironment.Object);
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
        public async Task Index_ReturnsViewWithAllItems()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUser(context);
            
            // Act
            var result = await controller.Index();
            
            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Item>>(viewResult.Model);
            Assert.Equal(3, model.Count());
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
        public async Task Details_ReturnsNotFound_WhenItemDoesNotExist()
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
        public async Task Details_ReturnsViewResult_WithItem()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUser(context);
            
            // Act
            var result = await controller.Details(1);
            
            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Item>(viewResult.Model);
            Assert.Equal(1, model.Id);
            Assert.Equal("Laptop", model.Name);
        }
        
        [Fact]
        public async Task Create_ReturnsUnauthorized_WhenUserIsNotAdmin()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUser(context, isAdmin: false);
            
            // Act
            var result = controller.Create();
            
            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }
        
        [Fact]
        public void Create_ReturnsViewResult_WhenUserIsAdmin()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUser(context, isAdmin: true);
            
            // Act
            var result = controller.Create();
            
            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            
            // Verify that categories are passed to view
            Assert.NotNull(viewResult.ViewData["CategoryId"]);
        }
        
        [Fact]
        public async Task CreatePost_ReturnsUnauthorized_WhenUserIsNotAdmin()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUser(context, isAdmin: false);
            
            var newItem = new Item
            {
                Name = "New Item",
                Description = "Test item",
                Price = 100.00M,
                Quantity = 5,
                CategoryId = 1
            };
            
            // Act
            var result = await controller.Create(newItem, null);
            
            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }
        
        [Fact]
        public async Task CreatePost_RedirectsToIndex_WhenModelStateIsValid()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUser(context, isAdmin: true);
            
            var newItem = new Item
            {
                Name = "New Item",
                Description = "Test item",
                Price = 100.00M,
                Quantity = 5,
                CategoryId = 1
            };
            
            // Act
            var result = await controller.Create(newItem, null);
            
            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            
            // Verify item was added to database
            var item = await context.Items.FirstOrDefaultAsync(i => i.Name == "New Item");
            Assert.NotNull(item);
        }
        
        [Fact]
        public async Task CreatePost_ReturnsViewResult_WhenModelStateIsInvalid()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUser(context, isAdmin: true);
            
            var newItem = new Item
            {
                // Missing required fields
                Name = string.Empty,
                CategoryId = 1
            };
            
            // Add model error
            controller.ModelState.AddModelError("Name", "Name is required");
            
            // Act
            var result = await controller.Create(newItem, null);
            
            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(newItem, viewResult.Model);
            
            // Verify that categories are passed to view
            Assert.NotNull(viewResult.ViewData["CategoryId"]);
        }
        
        [Fact]
        public async Task Edit_ReturnsNotFound_WhenIdIsNull()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUser(context, isAdmin: true);
            
            // Act
            var result = await controller.Edit(null);
            
            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
        
        [Fact]
        public async Task Edit_ReturnsNotFound_WhenItemDoesNotExist()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUser(context, isAdmin: true);
            
            // Act
            var result = await controller.Edit(999);
            
            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
        
        [Fact]
        public async Task Edit_ReturnsUnauthorized_WhenUserIsNotAdmin()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUser(context, isAdmin: false);
            
            // Act
            var result = await controller.Edit(1);
            
            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }
        
        [Fact]
        public async Task Edit_ReturnsViewResult_WithItem()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUser(context, isAdmin: true);
            
            // Act
            var result = await controller.Edit(1);
            
            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Item>(viewResult.Model);
            Assert.Equal(1, model.Id);
            Assert.Equal("Laptop", model.Name);
            
            // Verify that categories are passed to view
            Assert.NotNull(viewResult.ViewData["CategoryId"]);
        }
        
        [Fact]
        public async Task EditPost_ReturnsNotFound_WhenIdDoesNotMatchItemId()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUser(context, isAdmin: true);
            
            // Act
            var result = await controller.Edit(99, new Item { Id = 1 }, null);
            
            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
        
        [Fact]
        public async Task EditPost_ReturnsUnauthorized_WhenUserIsNotAdmin()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUser(context, isAdmin: false);
            
            // Act
            var result = await controller.Edit(1, new Item { Id = 1 }, null);
            
            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }
        
        [Fact]
        public async Task EditPost_RedirectsToIndex_WhenModelStateIsValid()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUser(context, isAdmin: true);
            
            var existingItem = await context.Items.FindAsync(1);
            existingItem.Name = "Updated Laptop";
            existingItem.Price = 1300.00M;
            
            // Act
            var result = await controller.Edit(1, existingItem, null);
            
            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            
            // Verify item was updated in database
            var updatedItem = await context.Items.FindAsync(1);
            Assert.Equal("Updated Laptop", updatedItem.Name);
            Assert.Equal(1300.00M, updatedItem.Price);
        }
        
        [Fact]
        public async Task Delete_ReturnsNotFound_WhenIdIsNull()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUser(context, isAdmin: true);
            
            // Act
            var result = await controller.Delete(null);
            
            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
        
        [Fact]
        public async Task Delete_ReturnsNotFound_WhenItemDoesNotExist()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUser(context, isAdmin: true);
            
            // Act
            var result = await controller.Delete(999);
            
            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
        
        [Fact]
        public async Task Delete_ReturnsUnauthorized_WhenUserIsNotAdmin()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUser(context, isAdmin: false);
            
            // Act
            var result = await controller.Delete(1);
            
            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }
        
        [Fact]
        public async Task Delete_ReturnsViewResult_WithItem()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUser(context, isAdmin: true);
            
            // Act
            var result = await controller.Delete(1);
            
            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Item>(viewResult.Model);
            Assert.Equal(1, model.Id);
            Assert.Equal("Laptop", model.Name);
        }
        
        [Fact]
        public async Task DeleteConfirmed_ReturnsUnauthorized_WhenUserIsNotAdmin()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUser(context, isAdmin: false);
            
            // Act
            var result = await controller.DeleteConfirmed(1);
            
            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }
        
        [Fact]
        public async Task DeleteConfirmed_RemovesItem_AndRedirectsToIndex()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUser(context, isAdmin: true);
            
            // Act
            var result = await controller.DeleteConfirmed(1);
            
            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            
            // Verify item was removed from database
            var item = await context.Items.FindAsync(1);
            Assert.Null(item);
        }
    }
}
*/ 