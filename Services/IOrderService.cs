using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartInventoryManagement.Models;

namespace SmartInventoryManagement.Services
{
    /// <summary>
    /// Interface for order management operations
    /// </summary>
    public interface IOrderService
    {
        /// <summary>
        /// Get all orders with optional filtering
        /// </summary>
        Task<IEnumerable<Order>> GetOrdersAsync(string? searchString = null, 
            DateTime? startDate = null, DateTime? endDate = null, string? userId = null);
            
        /// <summary>
        /// Get an order by its ID
        /// </summary>
        Task<Order?> GetOrderByIdAsync(int id);
        
        /// <summary>
        /// Create a new order
        /// </summary>
        Task<Order> CreateOrderAsync(Order order);
        
        /// <summary>
        /// Update an existing order
        /// </summary>
        Task<Order> UpdateOrderAsync(Order order);
        
        /// <summary>
        /// Delete an order by its ID
        /// </summary>
        Task<bool> DeleteOrderAsync(int id);
        
        /// <summary>
        /// Add a product to an order
        /// </summary>
        Task<OrderItem> AddProductToOrderAsync(int orderId, int productId, int quantity);
        
        /// <summary>
        /// Remove a product from an order
        /// </summary>
        Task<bool> RemoveProductFromOrderAsync(int orderItemId);
        
        /// <summary>
        /// Check if an order exists by its ID
        /// </summary>
        Task<bool> OrderExistsAsync(int id);
        
        /// <summary>
        /// Get order items for an order
        /// </summary>
        Task<IEnumerable<OrderItem>> GetOrderItemsAsync(int orderId);
    }
} 