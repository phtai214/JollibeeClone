using Microsoft.EntityFrameworkCore;
using JollibeeClone.Data;
using JollibeeClone.Areas.Admin.Services;

var builder = WebApplication.CreateBuilder(args);

// Thêm DbContext với SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

// Add JSON serialization options
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

//  kích hoạt bộ nhớ tạm cho Session
builder.Services.AddDistributedMemoryCache();

//  đăng ký dịch vụ Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Đăng ký PromotionService
builder.Services.AddScoped<IPromotionService, PromotionService>();

// Thêm các service khác
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll"); // Sử dụng CORS policy
app.UseStaticFiles();

app.UseRouting();

app.UseSession(); //  Session phải được gọi sau UseRouting và trước UseAuthorization
app.UseAuthorization();

// Cấu hình route cho Areas (Admin) - route này phải đặt trước route mặc định
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// Route riêng cho admin với prefix /admin
app.MapControllerRoute(
    name: "admin",
    pattern: "admin/{action=Login}",
    defaults: new { area = "Admin", controller = "Auth" });

// Cấu hình route mặc định cho user (không có area)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
