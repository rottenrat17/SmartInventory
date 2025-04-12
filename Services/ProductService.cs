using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartInventoryManagement.Database;
using SmartInventoryManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartInventoryManagement.Services
{
    /// <summary>
    /// Service for managing products in the inventory system
    /// </summary>
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProductService"/> class.
        /// </summary>
        /// <param name="context">The database context</param>
        /// <param name="logger">The logger</param>
        public ProductService(ApplicationDbContext context, ILogger<ProductService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Product>> GetProductsAsync(string? searchString = null, int? categoryId = null, 
            decimal? minPrice = null, decimal? maxPrice = null, bool? lowStock = null)
        {
            _logger.LogInformation("Getting products with filters - SearchString: {SearchString}, CategoryId: {CategoryId}, MinPrice: {MinPrice}, MaxPrice: {MaxPrice}, LowStock: {LowStock}", 
                searchString, categoryId, minPrice, maxPrice, lowStock);
            
            var products = _context.Products.Include(p => p.Category).AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.Name.Contains(searchString));
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                products = products.Where(p => p.CategoryId == categoryId.Value);
            }

            if (minPrice.HasValue)
            {
                products = products.Where(p => p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                products = products.Where(p => p.Price <= maxPrice.Value);
            }
            
            if (lowStock.HasValue && lowStock.Value)
            {
                products = products.Where(p => p.StockQuantity <= p.LowStockThreshold);
            }

            return await products.ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<Product?> GetProductByIdAsync(int id)
        {
            _logger.LogInformation("Getting product with ID: {ProductId}", id);
            
            return await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        /// <inheritdoc/>
        public async Task<Product> CreateProductAsync(Product product)
        {
            _logger.LogInformation("Creating new product: {ProductName}", product.Name);
            
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            
            return product;
        }

        /// <inheritdoc/>
        public async Task<Product> UpdateProductAsync(Product product)
        {
            _logger.LogInformation("Updating product with ID: {ProductId}", product.Id);
            
            _context.Entry(product).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            
            return product;
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteProductAsync(int id)
        {
            _logger.LogInformation("Deleting product with ID: {ProductId}", id);
            
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                _logger.LogWarning("Product with ID {ProductId} not found for deletion", id);
                return false;
            }
            
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            
            return true;
        }

        /// <inheritdoc/>
        public async Task<Product?> UpdateStockAsync(int id, int newStock)
        {
            _logger.LogInformation("Updating stock for product ID: {ProductId} to {NewStock}", id, newStock);
            
            if (newStock < 0)
            {
                _logger.LogWarning("Attempted to set negative stock ({NewStock}) for product ID: {ProductId}", newStock, id);
                throw new ArgumentException("Stock quantity cannot be negative", nameof(newStock));
            }
            
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                _logger.LogWarning("Product with ID {ProductId} not found for stock update", id);
                return null;
            }
            
            product.StockQuantity = newStock;
            await _context.SaveChangesAsync();
            
            return product;
        }

        /// <inheritdoc/>
        public async Task<bool> ProductExistsAsync(int id)
        {
            return await _context.Products.AnyAsync(p => p.Id == id);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Category>> GetCategoriesAsync()
        {
            _logger.LogInformation("Getting all categories");
            
            return await _context.Categories.ToListAsync();
        }
    }
} 