/*
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Abstractions;
using System.Security.Claims;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using SmartInventoryManagement.Controllers;
using Microsoft.AspNetCore.Identity;
using SmartInventoryManagement.Models;
using Moq;
using Microsoft.Extensions.Logging;
using SmartInventoryManagement.Database;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using SmartInventoryManagement.Services;

namespace MyTestProject
{
    public class AuthorizationTests
    {
        [Fact]
        public async Task AdminRoleRequired_ForCreateProduct()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("AuthTestsDb")
                .Options;

            using var context = new ApplicationDbContext(options);
            var mockLogger = new Mock<ILogger<ProductsController>>();
            var mockEmailService = new Mock<IEmailService>();
            
            var controller = new ProductsController(context, mockEmailService.Object, mockLogger.Object);
            
            // Setup regular user claims
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "user@example.com"),
                new Claim(ClaimTypes.NameIdentifier, "user-id"),
                new Claim(ClaimTypes.Role, "User"),
            };
            
            var userIdentity = new ClaimsIdentity(userClaims, "Test");
            var userPrincipal = new ClaimsPrincipal(userIdentity);
            
            // Set up controller context
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = userPrincipal }
            };
            
            // Act - try to access Create action which should require Admin role
            var result = await Record.ExceptionAsync(() => controller.Create());
            
            // Assert
            Assert.NotNull(result);
            Assert.IsType<Exception>(result);
            // The exception will vary based on implementation, but there should be one since regular user
            // shouldn't be able to access admin-only methods
        }
        
        [Fact]
        public async Task RegularUser_CanAccess_ProductListing()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("AuthTestsDb2")
                .Options;

            using var context = new ApplicationDbContext(options);
            var mockLogger = new Mock<ILogger<ProductsController>>();
            var mockEmailService = new Mock<IEmailService>();
            
            // Add some test data
            context.Categories.Add(new Category { Id = 1, Name = "Test Category" });
            context.Products.Add(new Product
            {
                Id = 1,
                Name = "Test Product",
                CategoryId = 1,
                Price = 9.99m,
                StockQuantity = 10
            });
            await context.SaveChangesAsync();
            
            var controller = new ProductsController(context, mockEmailService.Object, mockLogger.Object);
            
            // Setup regular user claims
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "user@example.com"),
                new Claim(ClaimTypes.NameIdentifier, "user-id"),
                new Claim(ClaimTypes.Role, "User"),
            };
            
            var userIdentity = new ClaimsIdentity(userClaims, "Test");
            var userPrincipal = new ClaimsPrincipal(userIdentity);
            
            // Set up controller context
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = userPrincipal }
            };
            
            // Act - try to access Index action which should be accessible to all authenticated users
            var result = await controller.Index(null, null, null, null, null);
            
            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Product>>(viewResult.Model);
            Assert.Single(model); // Should see the one product we added
        }
    }
}
*/ 