using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using SmartInventoryManagement.Database;
using SmartInventoryManagement.Models;
using SmartInventoryManagement.Services;
using SmartInventoryManagement.Middleware;
using Serilog;
using Serilog.Events;
using System.IO;
using System.Linq;

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
                    serverOptions.ListenLocalhost(5002);
                });

                // Add services to the container.
                builder.Services.AddControllersWithViews();

                // Configure database with SQLite
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    // Use a default SQLite connection string if none is provided
                    connectionString = "Data Source=SmartInventory.db";
                    Log.Information("Using default SQLite connection string");
                }
                else
                {
                    // If using |DataDirectory| placeholder, make sure it's properly resolved
                    if (connectionString.Contains("|DataDirectory|"))
                    {
                        // For local development, replace |DataDirectory| with the App_Data folder
                        var appData = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data");
                        connectionString = connectionString.Replace("|DataDirectory|", appData);
                        Log.Information($"Resolved DataDirectory in connection string: {connectionString}");
                    }
                    else
                    {
                        Log.Information($"Using connection string from configuration: {connectionString}");
                    }
                }

                // Configure to use SQLite instead of PostgreSQL
                builder.Services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseSqlite(connectionString);
                });

                // Configure identity for database reset
                builder.Services.Configure<IdentityOptions>(options =>
                {
                    // Do not lockout users
                    options.Lockout.AllowedForNewUsers = false;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.Zero;
                    options.Lockout.MaxFailedAccessAttempts = 1000;
                });

                // Register Email Service - simple no-op implementation
                builder.Services.AddTransient<IEmailService, NoOpEmailService>();

                // Add Identity services
                builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
                {
                    // CRITICAL: Completely disable ALL email confirmation requirements
                    options.SignIn.RequireConfirmedAccount = false;
                    options.SignIn.RequireConfirmedEmail = false;
                    options.SignIn.RequireConfirmedPhoneNumber = false;
                    
                    // Password settings
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

                // Simple database initialization
                using (var scope = app.Services.CreateScope())
                {
                    try
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        dbContext.Database.EnsureCreated();
                        
                        var roleInitService = scope.ServiceProvider.GetRequiredService<RoleInitializationService>();
                        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                        
                        // Initialize roles and admin user
                        await roleInitService.InitializeRolesAsync(roleManager);
                        await roleInitService.InitializeAdminUserAsync(userManager);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error during initialization: {Message}", ex.Message);
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