using PRN222_CloneEbay_Seller.Models;
using PRN222_CloneEbay_Seller.Services.Interfaces;
using PRN222_CloneEbay_Seller.Services.Implementations;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add DbContext
builder.Services.AddDbContext<CloneEbayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Services
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IStoreService, StoreService>();
builder.Services.AddScoped<IPerformanceService, PerformanceService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Add specific route for Orders
app.MapControllerRoute(
    name: "orders",
    pattern: "Orders/{action=Index}/{id?}",
    defaults: new { controller = "Orders" });

// Add specific route for Stores
app.MapControllerRoute(
    name: "stores",
    pattern: "Stores/{action=Index}/{id?}",
    defaults: new { controller = "Stores" });

app.MapControllerRoute(
    name: "performance",
    pattern: "Performance/{action=Index}/{id?}",
    defaults: new { controller = "Performance" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Orders}/{action=Index}/{id?}");

app.Run();
