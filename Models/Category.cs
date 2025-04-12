using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartInventoryManagement.Models;

public class Category
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    public virtual ICollection<Product>? Products { get; set; }
}