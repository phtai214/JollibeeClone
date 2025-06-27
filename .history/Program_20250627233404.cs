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
builder.Services.AddControllersWithViews(options =>
{
    // Tăng giới hạn model binding cho combo với nhiều nhóm/options
    options.MaxModelBindingCollectionSize = 2048; // Default: 1024
})
.ConfigureApiBehaviorOptions(options =>
{
    // Tăng giới hạn cho complex models
    options.SuppressModelStateInvalidFilter = false;
});

// Cấu hình MVC options để hỗ trợ form phức tạp
builder.Services.Configure<Microsoft.AspNetCore.Mvc.MvcOptions>(options =>
{
    // Tăng giới hạn cho model binding
    options.MaxModelBindingCollectionSize = 2048;
    options.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor(value => "Trường này là bắt buộc.");
});

// Cấu hình Form Options để hỗ trợ nhiều fields
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.ValueCountLimit = 4096; // Default: 1024
    options.KeyLengthLimit = 4096; // Default: 2048  
    options.ValueLengthLimit = 1024 * 1024; // 1MB, Default: ~4MB
    options.MultipartBodyLengthLimit = 128 * 1024 * 1024; // 128MB
});

var app = builder.Build();

// ===== SEED DATA - TỰ ĐỘNG TẠO ADMIN CHO CẢ TEAM =====
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        
        // Đảm bảo database tồn tại
        await context.Database.EnsureCreatedAsync();
        
        // Seed dữ liệu admin và dữ liệu mẫu
        await SeedAdminData.SeedAsync(context);
        
        // Seed dữ liệu biến thể sản phẩm
        await SeedAdminData.SeedProductVariantsDirectAsync(context);
        
        Console.WriteLine("🎉 Seed data completed! Admin account and product variants ready for all team members!");
        Console.WriteLine("📧 Email: admin@jollibee.com");
        Console.WriteLine("🔐 Password: admin123");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "❌ An error occurred while seeding the database.");
        Console.WriteLine($"❌ Seed error: {ex.Message}");
    }
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll"); 
app.UseStaticFiles();

app.UseRouting();

app.UseSession(); 
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
