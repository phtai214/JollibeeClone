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
    // EXTREME CONFIG: Support 15 groups x 10 options (150 total options)
    options.MaxModelBindingCollectionSize = 20480; // Tăng lên 20K để support mega combos
})
.ConfigureApiBehaviorOptions(options =>
{
    // Tăng giới hạn cho complex models
    options.SuppressModelStateInvalidFilter = false;
});

// Cấu hình MVC options để hỗ trợ mega combo forms
builder.Services.Configure<Microsoft.AspNetCore.Mvc.MvcOptions>(options =>
{
    // MEGA COMBO SUPPORT: 15 groups x 10 options = 150+ total options
    options.MaxModelBindingCollectionSize = 20480; // 20K collection limit
    options.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor(value => "Trường này là bắt buộc.");
});

// Cấu hình Form Options cho MEGA COMBO với 15 groups x 10 options
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    // Tính toán: 15 groups x 10 options x ~8 fields per option = ~1200 fields
    // Thêm các fields khác: combo info, images, etc = ~200 fields
    // Total estimate: ~1400 fields, safe margin x3 = ~4200
    options.ValueCountLimit = 25600; // 25K values cho safety margin
    options.KeyLengthLimit = 16384; // 16KB key length cho deep nesting
    options.ValueLengthLimit = 4 * 1024 * 1024; // 4MB cho large form values
    options.MultipartBodyLengthLimit = 512 * 1024 * 1024; // 512MB cho file uploads
    options.MultipartHeadersCountLimit = 4096; // 4K headers
    options.MultipartHeadersLengthLimit = 131072; // 128KB headers length
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
