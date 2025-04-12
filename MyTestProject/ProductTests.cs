using Xunit;
using SmartInventoryManagement.Models;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;

namespace MyTestProject
{
    public class ProductTests
    {
        [Fact]
        public void Product_IsLowStock_ReturnsTrue_WhenStockBelowThreshold()
        {
            // Arrange
            var product = new Product
            {
                Name = "Test Product",
                StockQuantity = 5,
                LowStockThreshold = 10
            };

            // Act & Assert
            Assert.True(product.IsLowStock);
        }

        [Fact]
        public void Product_IsLowStock_ReturnsFalse_WhenStockAboveThreshold()
        {
            // Arrange
            var product = new Product
            {
                Name = "Test Product",
                StockQuantity = 15,
                LowStockThreshold = 10
            };

            // Act & Assert
            Assert.False(product.IsLowStock);
        }

        [Fact]
        public void Product_IsLowStock_ReturnsTrue_WhenStockEqualsThreshold()
        {
            // Arrange
            var product = new Product
            {
                Name = "Test Product",
                StockQuantity = 10,
                LowStockThreshold = 10
            };

            // Act & Assert
            Assert.True(product.IsLowStock);
        }

        [Fact]
        public void Product_Validation_Name_IsRequired()
        {
            // Arrange
            var product = new Product
            {
                Name = null,
                Price = 10.99m,
                StockQuantity = 20
            };

            // Act
            var validationResults = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(product, new ValidationContext(product), validationResults, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(validationResults, vr => vr.MemberNames.Contains("Name"));
        }

        [Fact]
        public void Product_Validation_Price_MustBePositive()
        {
            // Arrange
            var product = new Product
            {
                Name = "Test Product",
                Price = -10.99m,
                StockQuantity = 20
            };

            // Act
            var validationResults = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(product, new ValidationContext(product), validationResults, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(validationResults, vr => vr.MemberNames.Contains("Price"));
        }
    }
} 