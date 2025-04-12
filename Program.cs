using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using SmartInventoryManagement.Database;
using SmartInventoryManagement.Models;
using SmartInventoryManagement.Services;
using SmartInventoryManagement.Middleware;
using Npgsql;
using Serilog;
using Serilog.Events;

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

                // Configure Npgsql to use UTC DateTime for PostgreSQL
                AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
                AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);

                var builder = WebApplication.CreateBuilder(args);

                // Configure Kestrel to explicitly listen on port 5002
                builder.WebHost.ConfigureKestrel(serverOptions =>
                {
                    serverOptions.ListenAnyIP(5002);
                });

                // Add services to the container.
                builder.Services.AddControllersWithViews();

                // Configure database
                var connectionString = Environment.GetEnvironmentVariable("AZURE_POSTGRESQL_CONNECTIONSTRING");
                
                if (string.IsNullOrEmpty(connectionString))
                {
                    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                    Log.Information("Using connection string from configuration");
                }
                else
                {
                    Log.Information("Using connection string from environment variable");
                }
                
                if (string.IsNullOrEmpty(connectionString))
                {
                    Log.Error("No valid connection string found. Please check your configuration.");
                    throw new InvalidOperationException("No valid database connection string found.");
                }

                // Don't log connection string details for security
                Log.Information("Database connection configured");

                builder.Services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseNpgsql(connectionString, npgsqlOptions =>
                    {
                        npgsqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorCodesToAdd: null);
                    });
                });

                // Register Email Service
                builder.Services.AddTransient<IEmailService, MailerSendEmailService>();

                // Add Identity services
                builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
                    options.SignIn.RequireConfirmedAccount = true;
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireNonAlphanumeric = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequiredLength = 8;
                    options.User.RequireUniqueEmail = true;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

                // Configure cookie settings
                builder.Services.ConfigureApplicationCookie(options => {
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.SlidingExpiration = true;
                    options.ExpireTimeSpan = TimeSpan.FromDays(7);
                });

                // Configure Identity token lifespan
                builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
                {
                    options.TokenLifespan = TimeSpan.FromHours(24);
                });

                // Add Serilog
                builder.Host.UseSerilog();

                var app = builder.Build();

                // Configure the HTTP request pipeline.
                if (!app.Environment.IsDevelopment())
                {
                    app.UseExceptionHandler("/Error");
                    app.UseStatusCodePagesWithReExecute("/Error/{0}");
                    app.UseHsts();
                }
                else
                {
                    app.UseDeveloperExceptionPage();
                }

                app.UseHttpsRedirection();
                app.UseStaticFiles();
                app.UseRouting();

                app.UseAuthentication();
                app.UseAuthorization();

                // Add custom exception handling middleware
                app.UseMiddleware<ExceptionHandlingMiddleware>();

                app.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                // Ensure database exists and apply migrations
                try
                {
                    using (var scope = app.Services.CreateScope())
                    {
                        var services = scope.ServiceProvider;
                        var context = services.GetRequiredService<ApplicationDbContext>();
                        
                        Log.Information("Ensuring database exists and is up to date...");
                        
                        // Check if database exists
                        bool dbExists = await context.Database.CanConnectAsync();
                        
                        if (!dbExists)
                        {
                            Log.Information("Database does not exist. Creating database...");
                            await context.Database.EnsureCreatedAsync();
                        }
                        else
                        {
                            Log.Information("Database already exists. Checking for pending migrations...");
                            if ((await context.Database.GetPendingMigrationsAsync()).Any())
                            {
                                Log.Information("Applying pending migrations...");
                                await context.Database.MigrateAsync();
                            }
                        }

                        // Initialize roles
                        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

                        // Create roles if they don't exist
                        string[] roles = { "Admin", "User" };
                        foreach (var role in roles)
                        {
                            if (!await roleManager.RoleExistsAsync(role))
                            {
                                await roleManager.CreateAsync(new IdentityRole(role));
                                Log.Information("Created role: {Role}", role);
                            }
                        }

                        // Create admin user if it doesn't exist
                        var adminEmail = "admin@example.com";
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
                            var result = await userManager.CreateAsync(adminUser, "Admin123!");
                            if (result.Succeeded)
                            {
                                await userManager.AddToRoleAsync(adminUser, "Admin");
                                Log.Information("Created admin user: {Email}", adminEmail);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "An error occurred while initializing the database");
                    throw;
                }

                Log.Information("Application startup complete. Running the application...");
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