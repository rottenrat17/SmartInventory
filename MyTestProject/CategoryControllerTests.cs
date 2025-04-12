/*
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartInventoryManagement.Database;
using SmartInventoryManagement.Controllers;
using SmartInventoryManagement.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Moq;

namespace MyTestProject
{
    public class CategoryControllerTests
    {
        private ApplicationDbContext GetTestDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("CategoryControllerTestDb" + System.Guid.NewGuid().ToString())
                .Options;
            
            var context = new ApplicationDbContext(options);
            
            // Add test categories
            context.Categories.AddRange(
                new Category { Id = 1, Name = "Electronics", Description = "Electronic devices" },
                new Category { Id = 2, Name = "Furniture", Description = "Home furniture" },
                new Category { Id = 3, Name = "Books", Description = "Books and magazines" }
            );
            
            context.SaveChanges();
            return context;
        }

        private CategoryController SetupControllerWithUserRole(ApplicationDbContext context, string role)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "testuser"),
                new Claim(ClaimTypes.Name, "Test User"),
                new Claim(ClaimTypes.Role, role)
            }, "mock"));

            var controller = new CategoryController(context);
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
        public async Task Index_ReturnsAllCategories()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUserRole(context, "User");
            
            // Act
            var result = await controller.Index();
            
            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Category>>(viewResult.Model);
            Assert.Equal(3, model.Count());
        }
        
        [Fact]
        public async Task Details_ReturnsNotFound_WhenIdIsNull()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUserRole(context, "User");
            
            // Act
            var result = await controller.Details(null);
            
            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
        
        [Fact]
        public async Task Details_ReturnsNotFound_WhenCategoryDoesNotExist()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUserRole(context, "User");
            
            // Act
            var result = await controller.Details(999);
            
            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
        
        [Fact]
        public async Task Details_ReturnsViewResult_WithCategory()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUserRole(context, "User");
            
            // Act
            var result = await controller.Details(1);
            
            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Category>(viewResult.Model);
            Assert.Equal(1, model.Id);
            Assert.Equal("Electronics", model.Name);
        }
        
        [Fact]
        public async Task Create_ReturnsUnauthorized_WhenUserIsNotAdmin()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUserRole(context, "User");
            
            // Act
            var result = await controller.Create();
            
            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }
        
        [Fact]
        public async Task Create_ReturnsViewResult_WhenUserIsAdmin()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUserRole(context, "Admin");
            
            // Act
            var result = await controller.Create();
            
            // Assert
            Assert.IsType<ViewResult>(result);
        }
        
        [Fact]
        public async Task CreatePost_RedirectsToIndex_WhenModelStateIsValid()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUserRole(context, "Admin");
            var newCategory = new Category 
            { 
                Name = "Tools", 
                Description = "Hand and power tools" 
            };
            
            // Act
            var result = await controller.Create(newCategory);
            
            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            
            // Verify category was added to the database
            var addedCategory = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Tools");
            Assert.NotNull(addedCategory);
            Assert.Equal("Hand and power tools", addedCategory.Description);
        }
        
        [Fact]
        public async Task CreatePost_ReturnsViewResult_WhenModelStateIsInvalid()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUserRole(context, "Admin");
            var newCategory = new Category(); // Invalid because Name is required
            controller.ModelState.AddModelError("Name", "Name is required");
            
            // Act
            var result = await controller.Create(newCategory);
            
            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(newCategory, viewResult.Model);
        }
        
        [Fact]
        public async Task EditGet_ReturnsNotFound_WhenIdIsNull()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUserRole(context, "Admin");
            
            // Act
            var result = await controller.Edit(null);
            
            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
        
        [Fact]
        public async Task EditGet_ReturnsNotFound_WhenCategoryDoesNotExist()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUserRole(context, "Admin");
            
            // Act
            var result = await controller.Edit(999);
            
            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
        
        [Fact]
        public async Task EditGet_ReturnsUnauthorized_WhenUserIsNotAdmin()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUserRole(context, "User");
            
            // Act
            var result = await controller.Edit(1);
            
            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }
        
        [Fact]
        public async Task EditGet_ReturnsViewResult_WithCategory()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUserRole(context, "Admin");
            
            // Act
            var result = await controller.Edit(1);
            
            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Category>(viewResult.Model);
            Assert.Equal(1, model.Id);
            Assert.Equal("Electronics", model.Name);
        }
        
        [Fact]
        public async Task EditPost_RedirectsToIndex_WhenModelStateIsValid()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUserRole(context, "Admin");
            var category = await context.Categories.FirstAsync(c => c.Id == 1);
            category.Name = "Updated Electronics";
            category.Description = "Updated description";
            
            // Act
            var result = await controller.Edit(1, category);
            
            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            
            // Verify category was updated in the database
            var updatedCategory = await context.Categories.FirstAsync(c => c.Id == 1);
            Assert.Equal("Updated Electronics", updatedCategory.Name);
            Assert.Equal("Updated description", updatedCategory.Description);
        }
        
        [Fact]
        public async Task DeleteGet_ReturnsNotFound_WhenIdIsNull()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUserRole(context, "Admin");
            
            // Act
            var result = await controller.Delete(null);
            
            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
        
        [Fact]
        public async Task DeleteGet_ReturnsNotFound_WhenCategoryDoesNotExist()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUserRole(context, "Admin");
            
            // Act
            var result = await controller.Delete(999);
            
            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
        
        [Fact]
        public async Task DeleteGet_ReturnsUnauthorized_WhenUserIsNotAdmin()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUserRole(context, "User");
            
            // Act
            var result = await controller.Delete(1);
            
            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }
        
        [Fact]
        public async Task DeleteGet_ReturnsViewResult_WithCategory()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUserRole(context, "Admin");
            
            // Act
            var result = await controller.Delete(1);
            
            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Category>(viewResult.Model);
            Assert.Equal(1, model.Id);
            Assert.Equal("Electronics", model.Name);
        }
        
        [Fact]
        public async Task DeleteConfirmed_RemovesCategory_AndRedirectsToIndex()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUserRole(context, "Admin");
            
            // Act
            var result = await controller.DeleteConfirmed(2);
            
            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            
            // Verify category was removed from the database
            Assert.Null(await context.Categories.FindAsync(2));
            Assert.Equal(2, await context.Categories.CountAsync()); // 3 categories - 1 deleted = 2 remaining
        }
    }
}
*/ 