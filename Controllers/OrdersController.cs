using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartInventoryManagement.Database;
using SmartInventoryManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace SmartInventoryManagement.Controllers
{
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Order
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Orders.Include(o => o.OrderItems).ThenInclude(oi => oi.Product).ToListAsync());
        }

        // GET: Order/Track
        [AllowAnonymous]
        public IActionResult Track()
        {
            return View();
        }

        // POST: Order/Track
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Track(string orderNumber)
        {
            if (string.IsNullOrEmpty(orderNumber))
            {
                return View();
            }

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

            if (order == null)
            {
                ViewBag.ErrorMessage = "Order not found. Please check the order number and try again.";
                return View();
            }

            return View("OrderSummary", order);
        }

        // GET: Order/Create
        [AllowAnonymous]
        public async Task<IActionResult> Create()
        {
            ViewBag.Products = await _context.Products.Where(p => p.StockQuantity > 0).ToListAsync();
            return View();
        }

        // POST: Order/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Create(string guestName, string guestEmail, List<int> productIds, List<int> quantities)
        {
            try
            {
                if (productIds == null || quantities == null || productIds.Count != quantities.Count)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, errorMessage = "Invalid order data. Please try again." });
                    }
                    
                    ViewBag.ErrorMessage = "Invalid order data. Please try again.";
                    ViewBag.Products = await _context.Products.Where(p => p.StockQuantity > 0).ToListAsync();
                    return View();
                }

                // Create a new order
                var order = new Order
                {
                    GuestName = guestName,
                    GuestEmail = guestEmail,
                    OrderDate = DateTime.UtcNow,
                    OrderNumber = GenerateOrderNumber(),
                    OrderItems = new List<OrderItem>()
                };

                decimal totalAmount = 0;

                // Add order items
                for (int i = 0; i < productIds.Count; i++)
                {
                    if (quantities[i] <= 0) continue;

                    var product = await _context.Products.FindAsync(productIds[i]);
                    if (product == null || product.StockQuantity < quantities[i])
                    {
                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        {
                            return Json(new { success = false, errorMessage = "One or more products are unavailable in the requested quantity." });
                        }
                        
                        ViewBag.ErrorMessage = "One or more products are unavailable in the requested quantity.";
                        ViewBag.Products = await _context.Products.Where(p => p.StockQuantity > 0).ToListAsync();
                        return View();
                    }

                    var orderItem = new OrderItem
                    {
                        ProductId = product.Id,
                        Product = product,
                        Quantity = quantities[i],
                        UnitPrice = product.Price
                    };

                    order.OrderItems.Add(orderItem);
                    totalAmount += orderItem.UnitPrice * orderItem.Quantity;

                    // Update product stock
                    product.StockQuantity -= quantities[i];
                }

                order.TotalAmount = totalAmount;

                // Save order to database
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { 
                        success = true, 
                        orderId = order.Id,
                        orderNumber = order.OrderNumber
                    });
                }
                
                return RedirectToAction(nameof(OrderSummary), new { id = order.Id });
            }
            catch (Exception ex)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, errorMessage = $"An error occurred while processing your order: {ex.Message}" });
                }
                
                ViewBag.ErrorMessage = $"An error occurred while processing your order: {ex.Message}";
                ViewBag.Products = await _context.Products.Where(p => p.StockQuantity > 0).ToListAsync();
                return View();
            }
        }

        // GET: Order/OrderSummary/5
        [AllowAnonymous]
        public async Task<IActionResult> OrderSummary(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        private string GenerateOrderNumber()
        {
            // Generate a unique order number, e.g., ORD-YYYYMMDD-XXXX
            string dateSegment = DateTime.UtcNow.ToString("yyyyMMdd");
            string randomSegment = new Random().Next(1000, 10000).ToString();
            return $"ORD-{dateSegment}-{randomSegment}";
        }
    }
}