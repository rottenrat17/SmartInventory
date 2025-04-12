using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartInventoryManagement.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        
        private DateTime _dateJoined = DateTime.UtcNow;
        
        public DateTime DateJoined 
        { 
            get => _dateJoined;
            set => _dateJoined = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }
    }
} 