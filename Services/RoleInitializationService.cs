using Microsoft.AspNetCore.Identity;
using SmartInventoryManagement.Models;

namespace SmartInventoryManagement.Services
{
    public class RoleInitializationService
    {
        public async Task InitializeRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            // Create roles if they don't exist
            string[] roleNames = { "Admin", "User" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }

        public async Task InitializeAdminUserAsync(UserManager<ApplicationUser> userManager)
        {
            // Create admin user if it doesn't exist
            var adminEmail = "pokhrelpratik71@gmail.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FirstName = "Admin",
                    LastName = "User"
                };

                // Create the admin user with the specified password
                var result = await userManager.CreateAsync(adminUser, "gaFk7Udp?q1");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
            else if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                // If user exists but not in Admin role, add them to Admin role
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
    }
} 