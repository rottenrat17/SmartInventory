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
                    try
                    {
                        Log.Information("Configuring database with connection string: {ConnectionString}", 
                            connectionString.Substring(0, Math.Min(10, connectionString.Length)) + "...");
                        
                        options.UseSqlite(connectionString);
                        Log.Information("Database context configured with SQLite");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error configuring database context: {Message}", ex.Message);
                        // Continue with default options - will be caught by error handling later
                        options.UseSqlite("Data Source=SmartInventory_fallback.db");
                        Log.Warning("Using fallback database configuration");
                    }
                });

                // Configure identity for database reset
                builder.Services.Configure<IdentityOptions>(options =>
                {
                    // Do not lockout users
                    options.Lockout.AllowedForNewUsers = false;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.Zero;
                    options.Lockout.MaxFailedAccessAttempts = 1000;
                });

                // Register Email Service - Use NoOp service to completely disable email functionality
                // This ensures that no email-related errors can prevent the application from working
                builder.Services.AddTransient<IEmailService>(sp => 
                {
                    var logger = sp.GetRequiredService<ILogger<NoOpEmailService>>();
                    return new NoOpEmailService(logger);
                });

                // Add Identity services
                builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
                {
                    // CRITICAL FIX: Completely disable email confirmation requirements
                    options.SignIn.RequireConfirmedAccount = builder.Configuration.GetValue<bool>("IdentitySettings:RequireConfirmedAccount", false);
                    options.SignIn.RequireConfirmedEmail = builder.Configuration.GetValue<bool>("IdentitySettings:RequireConfirmedEmail", false);
                    options.SignIn.RequireConfirmedPhoneNumber = builder.Configuration.GetValue<bool>("IdentitySettings:RequireConfirmedPhoneNumber", false);
                    
                    // Log this configuration at startup
                    Log.Information("STARTUP CONFIG: Email confirmation settings - RequireConfirmedAccount:{0}, RequireConfirmedEmail:{1}", 
                        options.SignIn.RequireConfirmedAccount, 
                        options.SignIn.RequireConfirmedEmail);
                    
                    // User settings - allow duplicate emails temporarily
                    options.User.RequireUniqueEmail = false;
                    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                    
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
                    // For production - still show detailed errors for now to diagnose issues
                    app.UseDeveloperExceptionPage();
                    // We'll also leave these as fallbacks
                    app.UseExceptionHandler("/Error/Error");
                    app.UseStatusCodePagesWithReExecute("/Error/{0}");
                    app.UseHsts();
                    
                    // Log that we're running in Production mode
                    Log.Information("Application is running in Production environment with DeveloperExceptionPage enabled for diagnostics");
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
                    try
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        
                        // Create the database if it doesn't exist
                        Log.Information("Attempting to create database if it doesn't exist");
                        
                        // First, check if we can connect to the database
                        bool canConnect = false;
                        try
                        {
                            canConnect = dbContext.Database.CanConnect();
                            Log.Information("Database connection test result: {CanConnect}", canConnect);
                        }
                        catch (Exception connEx)
                        {
                            Log.Error(connEx, "Error testing database connection: {Message}", connEx.Message);
                            // We'll handle this below
                        }
                        
                        try
                        {
                            if (canConnect)
                            {
                                Log.Information("Ensuring database exists...");
                                dbContext.Database.EnsureCreated();
                                Log.Information("Database created or already exists");
                            }
                            else
                            {
                                Log.Warning("Cannot connect to database - skipping initialization");
                            }
                        }
                        catch (Exception dbEx)
                        {
                            Log.Error(dbEx, "Failed to ensure database is created: {Message}", dbEx.Message);
                        }
                        
                        // Proceed with role and user initialization regardless of database status
                        // This way the app can run even if database setup fails
                        Log.Information("Initializing application roles and users");
                        try
                        {
                            var roleInitService = scope.ServiceProvider.GetRequiredService<RoleInitializationService>();
                            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                            
                            Log.Information("Initializing roles");
                            try
                            {
                                await roleInitService.InitializeRolesAsync(roleManager);
                                Log.Information("Roles initialized successfully");
                            }
                            catch (Exception roleEx)
                            {
                                Log.Error(roleEx, "Failed to initialize roles: {Message}", roleEx.Message);
                            }
                            
                            Log.Information("Initializing admin user");
                            try
                            {
                                await roleInitService.InitializeAdminUserAsync(userManager);
                                Log.Information("Admin user initialized successfully");
                            }
                            catch (Exception userEx)
                            {
                                Log.Error(userEx, "Failed to initialize admin user: {Message}", userEx.Message);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error initializing roles or users: {Message}", ex.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error setting up application: {Message}", ex.Message);
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