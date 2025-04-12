using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using SmartInventoryManagement.Database;
using SmartInventoryManagement.Models;
using SmartInventoryManagement.Services;
using SmartInventoryManagement.Middleware;
using Serilog;
using Serilog.Events;
using System.IO;

namespace SmartInventoryManagement
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                // Configure Serilog
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .WriteTo.Console()
                    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
                    .CreateLogger();

                Log.Information("Starting up application...");

                // Set DataDirectory to a persistent folder in Azure
                string dataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data");
                if (!Directory.Exists(dataDirectory))
                {
                    Directory.CreateDirectory(dataDirectory);
                }
                AppDomain.CurrentDomain.SetData("DataDirectory", dataDirectory);
                Log.Information($"DataDirectory set to: {dataDirectory}");

                var builder = WebApplication.CreateBuilder(args);

                // Configure Kestrel to explicitly listen on port 5002
                builder.WebHost.ConfigureKestrel(serverOptions =>
                {
                    serverOptions.ListenAnyIP(5002);
                });

                // Add services to the container.
                builder.Services.AddControllersWithViews();

                // Configure database with SQLite
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    // Use a default SQLite connection string if none is provided
                    connectionString = "Data Source=|DataDirectory|/SmartInventory.db";
                    Log.Information("Using default SQLite connection string");
                }
                else
                {
                    Log.Information("Using connection string from configuration");
                }

                // Configure to use SQLite instead of PostgreSQL
                builder.Services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseSqlite(connectionString);
                });

                // Register Email Service
                builder.Services.AddTransient<IEmailService, MailerSendEmailService>();

                // Add Identity services
                builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
                {
                    options.SignIn.RequireConfirmedAccount = true;
                    options.User.RequireUniqueEmail = true;
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireNonAlphanumeric = true;
                    options.Password.RequiredLength = 8;
                })
                    .AddEntityFrameworkStores<ApplicationDbContext>()
                    .AddDefaultTokenProviders();

                // Configure Identity Cookie settings
                builder.Services.ConfigureApplicationCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.SlidingExpiration = true;
                    options.ExpireTimeSpan = TimeSpan.FromDays(7);
                });

                // Add services to DI container
                builder.Services.AddScoped<RoleInitializationService>();

                var app = builder.Build();

                // Configure the HTTP request pipeline.
                if (app.Environment.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                }
                else
                {
                    app.UseExceptionHandler("/Error/Error");
                    app.UseStatusCodePagesWithReExecute("/Error/{0}");
                    app.UseHsts();
                }

                // Use the custom exception handling middleware
                app.UseMiddleware<ExceptionHandlingMiddleware>();

                app.UseHttpsRedirection();
                app.UseStaticFiles();

                app.UseRouting();

                app.UseAuthentication();
                app.UseAuthorization();

                app.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                // Ensure the database is created and apply migrations
                using (var scope = app.Services.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    try
                    {
                        // Create the database if it doesn't exist
                        Log.Information("Attempting to create database if it doesn't exist");
                        dbContext.Database.EnsureCreated();
                        Log.Information("Database created or already exists");

                        // Initialize roles and users directly instead of using the service
                        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                        
                        // Create roles if they don't exist
                        string[] roleNames = { "Admin", "User" };
                        foreach (var roleName in roleNames)
                        {
                            if (!await roleManager.RoleExistsAsync(roleName))
                            {
                                await roleManager.CreateAsync(new IdentityRole(roleName));
                                Log.Information($"Created role: {roleName}");
                            }
                        }
                        
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
                                Log.Information($"Created admin user: {adminEmail}");
                            }
                            else
                            {
                                foreach (var error in result.Errors)
                                {
                                    Log.Error($"Error creating admin user: {error.Description}");
                                }
                            }
                        }
                        else if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                        {
                            // If user exists but not in Admin role, add them to Admin role
                            await userManager.AddToRoleAsync(adminUser, "Admin");
                            Log.Information($"Added existing user to Admin role: {adminEmail}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error setting up database");
                    }
                }

                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}