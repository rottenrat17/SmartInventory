using System.ComponentModel.DataAnnotations;

namespace SmartInventoryManagement.Models
{
    public class Product
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
        
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        
        [Range(0, 10000)]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }
        
        [Display(Name = "Stock Quantity")]
        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }
        
        [Display(Name = "Low Stock Threshold")]
        [Range(1, int.MaxValue)]
        public int LowStockThreshold { get; set; } = 10;

        public bool IsLowStock => StockQuantity <= LowStockThreshold;
    }
}

