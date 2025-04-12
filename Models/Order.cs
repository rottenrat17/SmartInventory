using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartInventoryManagement.Models
{
    public class Order
    {
        public int Id { get; set; }
        
        [Required]
        public string OrderNumber { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "Order Date")]
        [DataType(DataType.DateTime)]
        public DateTime OrderDate { get; set; }
        
        [Required]
        [StringLength(100)]
        [Display(Name = "Guest Name")]
        public string GuestName { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [StringLength(100)]
        [Display(Name = "Guest Email")]
        public string GuestEmail { get; set; } = string.Empty;
        
        [DataType(DataType.Currency)]
        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }
        
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}