using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartInventoryManagement.Database;
using SmartInventoryManagement.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartInventoryManagement.Services
{
    /// <summary>
    /// Service for managing categories in the inventory system
    /// </summary>
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CategoryService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CategoryService"/> class.
        /// </summary>
        /// <param name="context">The database context</param>
        /// <param name="logger">The logger</param>
        public CategoryService(ApplicationDbContext context, ILogger<CategoryService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Category>> GetCategoriesAsync()
        {
            _logger.LogInformation("Getting all categories");
            return await _context.Categories.ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            _logger.LogInformation("Getting category with ID: {CategoryId}", id);
            return await _context.Categories.FindAsync(id);
        }

        /// <inheritdoc/>
        public async Task<Category> CreateCategoryAsync(Category category)
        {
            _logger.LogInformation("Creating new category: {CategoryName}", category.Name);
            
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            
            return category;
        }

        /// <inheritdoc/>
        public async Task<Category> UpdateCategoryAsync(Category category)
        {
            _logger.LogInformation("Updating category with ID: {CategoryId}", category.Id);
            
            _context.Entry(category).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            
            return category;
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteCategoryAsync(int id)
        {
            _logger.LogInformation("Deleting category with ID: {CategoryId}", id);
            
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                _logger.LogWarning("Category with ID {CategoryId} not found for deletion", id);
                return false;
            }
            
            // Check if category has related products
            var hasProducts = await _context.Products.AnyAsync(p => p.CategoryId == id);
            if (hasProducts)
            {
                _logger.LogWarning("Cannot delete category with ID {CategoryId} because it has related products", id);
                throw new InvalidOperationException("Cannot delete a category that has related products");
            }
            
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            
            return true;
        }

        /// <inheritdoc/>
        public async Task<bool> CategoryExistsAsync(int id)
        {
            return await _context.Categories.AnyAsync(c => c.Id == id);
        }
    }
} 