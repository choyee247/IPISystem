using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
//using ProjectManagementSystem.Data;
using ProjectManagementSystem.DBModels;
using ProjectManagementSystem.Models;
using ProjectManagementSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();
// Add Session and MemoryCache
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configure DbContext
builder.Services.AddDbContext<PMSDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Identity (MUST be before builder.Build())
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<PMSDbContext>()
    .AddDefaultTokenProviders();

// Add this to your services configuration
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(30); // Persistent cookie duration
    options.SlidingExpiration = true;
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession();

builder.Services.AddScoped<ProjectManagementSystem.Services.Interface.IActivityLogger,
                           ProjectManagementSystem.Services.ActivityLogger>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 1;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequiredUniqueChars = 0;
});

var app = builder.Build();
// SEED DATA
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ProjectManagementSystem.DBModels.PMSDbContext>();
    DbInitializer.SeedAcademicYears(context);
}
// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession(); // ? Enable Session Middleware

// Authentication MUST come before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.UseExceptionHandler("/Home/Error");
app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");

// Database Seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await IdentitySeeder.SeedRolesAndAdminsAsync(services);
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Welcome}/{action=Index}/{id?}");
    //pattern: "{controller=Admin}/{action=Login}/{id?}");


app.Run(); 