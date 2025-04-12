using System.Collections.Generic;
using System.Threading.Tasks;
using SmartInventoryManagement.Models;

namespace SmartInventoryManagement.Services
{
    /// <summary>
    /// Interface for product management operations
    /// </summary>
    public interface IProductService
    {
        /// <summary>
        /// Get all products with optional filtering
        /// </summary>
        Task<IEnumerable<Product>> GetProductsAsync(string? searchString = null, int? categoryId = null, 
            decimal? minPrice = null, decimal? maxPrice = null, bool? lowStock = null);
            
        /// <summary>
        /// Get a product by its ID
        /// </summary>
        Task<Product?> GetProductByIdAsync(int id);
        
        /// <summary>
        /// Create a new product
        /// </summary>
        Task<Product> CreateProductAsync(Product product);
        
        /// <summary>
        /// Update an existing product
        /// </summary>
        Task<Product> UpdateProductAsync(Product product);
        
        /// <summary>
        /// Delete a product by its ID
        /// </summary>
        Task<bool> DeleteProductAsync(int id);
        
        /// <summary>
        /// Update the stock quantity of a product
        /// </summary>
        Task<Product?> UpdateStockAsync(int id, int newStock);
        
        /// <summary>
        /// Check if a product exists by its ID
        /// </summary>
        Task<bool> ProductExistsAsync(int id);
        
        /// <summary>
        /// Get all categories
        /// </summary>
        Task<IEnumerable<Category>> GetCategoriesAsync();
    }
} 