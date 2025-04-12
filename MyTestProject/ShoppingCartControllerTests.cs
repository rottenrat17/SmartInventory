/*
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartInventoryManagement.Database;
using SmartInventoryManagement.Controllers;
using SmartInventoryManagement.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Moq;

namespace MyTestProject
{
    public class ShoppingCartControllerTests
    {
        private ApplicationDbContext GetTestDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("ShoppingCartControllerTestDb" + System.Guid.NewGuid().ToString())
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
            
            // Add test shopping cart
            context.ShoppingCarts.Add(new ShoppingCart { Id = 1, UserId = "testuser" });
            
            context.SaveChanges();
            return context;
        }

        private ShoppingCartController SetupControllerWithUser(ApplicationDbContext context, string userId = "testuser")
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, "Test User")
            }, "mock"));

            var controller = new ShoppingCartController(context);
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
        public async Task Index_CreatesNewCart_WhenUserHasNoCart()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUser(context, "newuser");
            
            // Act
            var result = await controller.Index();
            
            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            
            // Verify a new cart was created
            var cart = await context.ShoppingCarts.FirstOrDefaultAsync(c => c.UserId == "newuser");
            Assert.NotNull(cart);
        }
        
        [Fact]
        public async Task Index_ReturnsViewWithCartItems()
        {
            // Arrange
            using var context = GetTestDbContext();
            var cart = await context.ShoppingCarts.FirstAsync(c => c.UserId == "testuser");
            
            // Add items to cart
            context.CartItems.AddRange(
                new CartItem { Id = 1, ShoppingCartId = cart.Id, ItemId = 1, Quantity = 2 },
                new CartItem { Id = 2, ShoppingCartId = cart.Id, ItemId = 3, Quantity = 1 }
            );
            await context.SaveChangesAsync();
            
            var controller = SetupControllerWithUser(context);
            
            // Act
            var result = await controller.Index();
            
            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<CartItem>>(viewResult.Model);
            Assert.Equal(2, model.Count);
        }
        
        [Fact]
        public async Task AddToCart_AddsItemToCart_WhenItemExists()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUser(context);
            
            // Act
            var result = await controller.AddToCart(2); // Add smartphone to cart
            
            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            
            // Verify item was added to cart
            var cart = await context.ShoppingCarts.FirstAsync(c => c.UserId == "testuser");
            var cartItem = await context.CartItems.FirstOrDefaultAsync(ci => ci.ShoppingCartId == cart.Id && ci.ItemId == 2);
            Assert.NotNull(cartItem);
            Assert.Equal(1, cartItem.Quantity);
        }
        
        [Fact]
        public async Task AddToCart_IncreasesQuantity_WhenItemAlreadyInCart()
        {
            // Arrange
            using var context = GetTestDbContext();
            var cart = await context.ShoppingCarts.FirstAsync(c => c.UserId == "testuser");
            
            // Add item to cart
            context.CartItems.Add(new CartItem { Id = 1, ShoppingCartId = cart.Id, ItemId = 2, Quantity = 1 });
            await context.SaveChangesAsync();
            
            var controller = SetupControllerWithUser(context);
            
            // Act
            var result = await controller.AddToCart(2); // Add smartphone to cart again
            
            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            
            // Verify quantity was increased
            var cartItem = await context.CartItems.FirstAsync(ci => ci.ShoppingCartId == cart.Id && ci.ItemId == 2);
            Assert.Equal(2, cartItem.Quantity);
        }
        
        [Fact]
        public async Task AddToCart_ReturnsNotFound_WhenItemDoesNotExist()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUser(context);
            
            // Act
            var result = await controller.AddToCart(999); // Non-existent item
            
            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
        
        [Fact]
        public async Task RemoveFromCart_RemovesItemFromCart()
        {
            // Arrange
            using var context = GetTestDbContext();
            var cart = await context.ShoppingCarts.FirstAsync(c => c.UserId == "testuser");
            
            // Add items to cart
            context.CartItems.AddRange(
                new CartItem { Id = 1, ShoppingCartId = cart.Id, ItemId = 1, Quantity = 2 },
                new CartItem { Id = 2, ShoppingCartId = cart.Id, ItemId = 3, Quantity = 1 }
            );
            await context.SaveChangesAsync();
            
            var controller = SetupControllerWithUser(context);
            
            // Act
            var result = await controller.RemoveFromCart(1); // Remove laptop from cart
            
            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            
            // Verify item was removed from cart
            var cartItem = await context.CartItems.FirstOrDefaultAsync(ci => ci.ShoppingCartId == cart.Id && ci.ItemId == 1);
            Assert.Null(cartItem);
            
            // Verify other item is still in cart
            var remainingItem = await context.CartItems.FirstOrDefaultAsync(ci => ci.ShoppingCartId == cart.Id && ci.ItemId == 3);
            Assert.NotNull(remainingItem);
        }
        
        [Fact]
        public async Task RemoveFromCart_ReturnsNotFound_WhenCartItemDoesNotExist()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUser(context);
            
            // Act
            var result = await controller.RemoveFromCart(999); // Non-existent cart item
            
            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
        
        [Fact]
        public async Task Checkout_CompletesOrderAndClearsCart()
        {
            // Arrange
            using var context = GetTestDbContext();
            var cart = await context.ShoppingCarts.FirstAsync(c => c.UserId == "testuser");
            
            // Add items to cart
            context.CartItems.AddRange(
                new CartItem { Id = 1, ShoppingCartId = cart.Id, ItemId = 1, Quantity = 2, Item = await context.Items.FindAsync(1) },
                new CartItem { Id = 2, ShoppingCartId = cart.Id, ItemId = 3, Quantity = 1, Item = await context.Items.FindAsync(3) }
            );
            await context.SaveChangesAsync();
            
            var controller = SetupControllerWithUser(context);
            
            // Act
            var result = await controller.Checkout();
            
            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("OrderConfirmation", redirectToActionResult.ActionName);
            
            // Verify order was created
            var order = await context.Orders.FirstOrDefaultAsync(o => o.UserId == "testuser");
            Assert.NotNull(order);
            
            // Verify order items were created
            var orderItems = await context.OrderItems.Where(oi => oi.OrderId == order.Id).ToListAsync();
            Assert.Equal(2, orderItems.Count);
            
            // Verify cart was cleared
            var cartItems = await context.CartItems.Where(ci => ci.ShoppingCartId == cart.Id).ToListAsync();
            Assert.Empty(cartItems);
            
            // Verify item quantities were updated
            var laptop = await context.Items.FindAsync(1);
            Assert.Equal(8, laptop.Quantity); // 10 - 2 = 8
            
            var chair = await context.Items.FindAsync(3);
            Assert.Equal(4, chair.Quantity); // 5 - 1 = 4
        }
        
        [Fact]
        public async Task Checkout_ReturnsViewResult_WhenCartIsEmpty()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUser(context);
            
            // Act
            var result = await controller.Checkout();
            
            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Error", viewResult.ViewName);
        }
        
        [Fact]
        public async Task OrderConfirmation_ReturnsViewResult()
        {
            // Arrange
            using var context = GetTestDbContext();
            var controller = SetupControllerWithUser(context);
            
            // Act
            var result = controller.OrderConfirmation();
            
            // Assert
            Assert.IsType<ViewResult>(result);
        }
    }
}
*/ 