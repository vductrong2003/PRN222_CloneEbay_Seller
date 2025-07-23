using PRN222_CloneEbay_Seller.Models;
using PRN222_CloneEbay_Seller.Services.Interfaces;
using PRN222_CloneEbay_Seller.Services.Implementations;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Lấy chuỗi kết nối từ appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Đăng ký DbContext với DI container
builder.Services.AddDbContext<CloneEbayDbContext>(options =>
    options.UseSqlServer(connectionString));

// Thêm các dịch vụ khác cho controller và view
builder.Services.AddControllersWithViews();

// Add DbContext
builder.Services.AddDbContext<CloneEbayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Services
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IStoreService, StoreService>();
builder.Services.AddScoped<IPerformanceService, PerformanceService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
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
