/*
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartInventoryManagement.Database;
using SmartInventoryManagement.Controllers;
using SmartInventoryManagement.Models;
using SmartInventoryManagement.ViewModels;
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
    public class UserControllerTests
    {
        private ApplicationDbContext GetTestDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("UserControllerTestDb" + System.Guid.NewGuid().ToString())
                .Options;
            
            var context = new ApplicationDbContext(options);
            
            // Seed with test users
            context.Users.AddRange(
                new User { Id = 1, Username = "admin", Password = "admin123", Email = "admin@example.com", Role = "Admin" },
                new User { Id = 2, Username = "user1", Password = "password123", Email = "user1@example.com", Role = "User" },
                new User { Id = 3, Username = "user2", Password = "password123", Email = "user2@example.com", Role = "User" }
            );
            
            context.SaveChanges();
            return context;
        }

        private UserController SetupControllerWithUserRole(ApplicationDbContext context, string role)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "testuser"),
                new Claim(ClaimTypes.Name, "Test User"),
                new Claim(ClaimTypes.Role, role)
            }, "mock"));

            var controller = new UserController(context);
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
        public void Login_ReturnsViewResult_WhenActionExecutes()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = new UserController(context);
            
            // Act
            var result = controller.Login();
            
            // Assert
            Assert.IsType<ViewResult>(result);
        }
        
        [Fact]
        public async Task Login_ReturnsRedirectToAction_WhenCredentialsAreValid()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = new UserController(context);
            var model = new LoginViewModel { Username = "admin", Password = "admin123" };
            
            // Act
            var result = await controller.Login(model);
            
            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal("Home", redirectToActionResult.ControllerName);
        }
        
        [Fact]
        public async Task Login_ReturnsViewResult_WhenCredentialsAreInvalid()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = new UserController(context);
            var model = new LoginViewModel { Username = "admin", Password = "wrongpassword" };
            
            // Act
            var result = await controller.Login(model);
            
            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
            Assert.False(controller.ModelState.IsValid);
        }
        
        [Fact]
        public void Register_ReturnsViewResult_WhenActionExecutes()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = new UserController(context);
            
            // Act
            var result = controller.Register();
            
            // Assert
            Assert.IsType<ViewResult>(result);
        }
        
        [Fact]
        public async Task Register_CreatesNewUser_WhenModelIsValid()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = new UserController(context);
            var model = new RegisterViewModel 
            { 
                Username = "newuser", 
                Password = "password123", 
                ConfirmPassword = "password123", 
                Email = "newuser@example.com" 
            };
            
            // Act
            var result = await controller.Register(model);
            
            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectToActionResult.ActionName);
            
            // Verify user was added to database
            var user = await context.Users.FirstOrDefaultAsync(u => u.Username == "newuser");
            Assert.NotNull(user);
            Assert.Equal("User", user.Role); // Default role should be "User"
        }
        
        [Fact]
        public async Task Register_ReturnsViewResult_WhenUsernameAlreadyExists()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = new UserController(context);
            var model = new RegisterViewModel 
            { 
                Username = "user1", // Existing username
                Password = "password123", 
                ConfirmPassword = "password123", 
                Email = "unique@example.com" 
            };
            
            // Act
            var result = await controller.Register(model);
            
            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
            Assert.False(controller.ModelState.IsValid);
        }
        
        [Fact]
        public async Task Register_ReturnsViewResult_WhenPasswordsDoNotMatch()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = new UserController(context);
            var model = new RegisterViewModel 
            { 
                Username = "newuser", 
                Password = "password123", 
                ConfirmPassword = "differentpassword", 
                Email = "newuser@example.com" 
            };
            
            // Act
            var result = await controller.Register(model);
            
            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
            Assert.False(controller.ModelState.IsValid);
        }
        
        [Fact]
        public void Logout_RedirectsToLogin()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = new UserController(context);
            
            // Act
            var result = controller.Logout();
            
            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectToActionResult.ActionName);
        }
    }
}
*/ 