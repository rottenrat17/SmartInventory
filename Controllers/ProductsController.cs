using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartInventoryManagement.Database;
using SmartInventoryManagement.Models;
using System.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using SmartInventoryManagement.Services;

namespace SmartInventoryManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(
            ApplicationDbContext context,
            IEmailService emailService,
            ILogger<ProductsController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        // GET: Products
        [AllowAnonymous]
        public async Task<IActionResult> Index(string searchString, int? categoryId, decimal? minPrice, decimal? maxPrice, bool? lowStock)
        {
            var products = _context.Products.Include(p => p.Category).AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.Name.Contains(searchString));
            }

            if (categoryId.HasValue)
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

            // Get categories for the filter dropdown
            ViewBag.Categories = await _context.Categories.ToListAsync();

            return View(await products.ToListAsync());
        }

        // GET: Products/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: Products/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(product);
        }

        // AJAX POST: Products/CreateAjax
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAjax(Product product)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    _logger.LogInformation("Creating new product via AJAX: {ProductName}", product.Name);
                    _context.Add(product);
                    await _context.SaveChangesAsync();
                    
                    return Json(new { success = true, message = "Product created successfully!" });
                }
                
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                
                return Json(new { success = false, errors = errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product via AJAX");
                return Json(new { success = false, message = "An error occurred while creating the product." });
            }
        }

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            
            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(product);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await ProductExists(id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(product);
        }

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // AJAX: Products/SearchProducts
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> SearchProducts(string searchTerm, int? categoryId, decimal? minPrice, decimal? maxPrice)
        {
            try
            {
                _logger.LogInformation("AJAX search request received. SearchTerm: {SearchTerm}, CategoryId: {CategoryId}", searchTerm, categoryId);
                
                var products = _context.Products.Include(p => p.Category).AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    products = products.Where(p => p.Name.Contains(searchTerm));
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

                var result = await products.Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Price,
                    p.StockQuantity,
                    CategoryName = p.Category?.Name ?? "No Category",
                    p.LowStockThreshold
                }).ToListAsync();

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AJAX search");
                return StatusCode(500, "An error occurred while searching for products");
            }
        }

        // AJAX: Products/GetProductPartial
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductPartial(string searchTerm, int? categoryId, decimal? minPrice, decimal? maxPrice)
        {
            var products = _context.Products.Include(p => p.Category).AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(searchTerm))
            {
                products = products.Where(p => p.Name.Contains(searchTerm));
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

            return PartialView("_ProductsList", await products.ToListAsync());
        }

        private async Task<bool> ProductExists(int id)
        {
            return await _context.Products.AnyAsync(e => e.Id == id);
        }
    }
}
