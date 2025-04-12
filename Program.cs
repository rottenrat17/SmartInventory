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
                    connectionString = "Data Source=SmartInventory.db";
                    Log.Information("Using default SQLite connection string");
                }
                else
                {
                    Log.Information($"Using connection string from configuration: {connectionString}");
                    
                    // Extract the database file path from the connection string
                    var dbFilePath = connectionString.Replace("Data Source=", "").Trim();
                    var dbDirectory = Path.GetDirectoryName(dbFilePath);
                    
                    // Ensure directory exists
                    if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
                    {
                        try {
                            Directory.CreateDirectory(dbDirectory);
                            Log.Information($"Created database directory: {dbDirectory}");
                        }
                        catch (Exception ex) {
                            Log.Error(ex, $"Failed to create database directory: {dbDirectory}");
                            // Fall back to default if we can't create the directory
                            connectionString = "Data Source=SmartInventory.db";
                            Log.Information("Falling back to default SQLite connection string");
                        }
                    }
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
                    try {
                        // Create the database if it doesn't exist
                        Log.Information("Attempting to create database if it doesn't exist");
                        dbContext.Database.EnsureCreated();
                        Log.Information("Database created or already exists");

                        // Initialize roles
                        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                        var roleInitializer = scope.ServiceProvider.GetRequiredService<RoleInitializationService>();

                        await roleInitializer.InitializeRolesAsync(roleManager);
                        await roleInitializer.InitializeAdminUserAsync(userManager);
                    }
                    catch (Exception ex) {
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