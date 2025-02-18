using Microsoft.AspNetCore.Mvc;
using SmartInventoryManagement.Data;
using SmartInventoryManagement.Models;

namespace SmartInventoryManagement.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Track()
        {
            var orders = _context.Orders.ToList();
            return View(Orders);
        }
    }
}