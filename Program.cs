using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StarEventsTicketingSystem.Data;
using StarEventsTicketingSystem.Models;
using StarEventsTicketingSystem.Utilities;

var builder1 = WebApplication.CreateBuilder(args);

// Bind Stripe section to configuration
builder1.Services.Configure<StripeSettings>(builder1.Configuration.GetSection("Stripe"));

var stripeSettings = builder1.Configuration.GetSection("Stripe");
builder1.Services.Configure<StripeSettings>(stripeSettings);

Stripe.StripeConfiguration.ApiKey = stripeSettings["SecretKey"];


var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// Register ApplicationDbContext with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity services
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure cookie settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Optional: Add dependency injection for utilities and custom services
// builder.Services.AddScoped<IEmailValidator, EmailValidator>();
// builder.Services.AddScoped<IPhoneValidator, PhoneValidator>();

var app = builder.Build();

// Seed roles
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    await RoleSeeder.SeedRolesAsync(roleManager);
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // Default 30 days
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Add Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
