using System.Collections.Generic;
using System.Threading.Tasks;
using SmartInventoryManagement.Models;

namespace SmartInventoryManagement.Services
{
    /// <summary>
    /// Interface for category management operations
    /// </summary>
    public interface ICategoryService
    {
        /// <summary>
        /// Get all categories
        /// </summary>
        Task<IEnumerable<Category>> GetCategoriesAsync();
        
        /// <summary>
        /// Get a category by its ID
        /// </summary>
        Task<Category?> GetCategoryByIdAsync(int id);
        
        /// <summary>
        /// Create a new category
        /// </summary>
        Task<Category> CreateCategoryAsync(Category category);
        
        /// <summary>
        /// Update an existing category
        /// </summary>
        Task<Category> UpdateCategoryAsync(Category category);
        
        /// <summary>
        /// Delete a category by its ID
        /// </summary>
        Task<bool> DeleteCategoryAsync(int id);
        
        /// <summary>
        /// Check if a category exists by its ID
        /// </summary>
        Task<bool> CategoryExistsAsync(int id);
    }
} 