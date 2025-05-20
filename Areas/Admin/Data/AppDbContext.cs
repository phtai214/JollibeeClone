using Microsoft.EntityFrameworkCore;
using JollibeeClone.Models;
using System.Data;
namespace JollibeeClone.Areas.Admin.Data
{
    public class AppDbContext:DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }
        // Khai báo DbSet cho các entity
        //public DbSet<Role> Roles { get; set; }
        //public DbSet<User> Users { get; set; }
        //public DbSet<UserRole> UserRoles { get; set; }
        //public DbSet<UserAddress> UserAddresses { get; set; }
        //public DbSet<Category> Categories { get; set; }
        //public DbSet<Product> Products { get; set; }
        //public DbSet<ProductConfigurationGroup> ProductConfigurationGroups { get; set; }
        //public DbSet<ProductConfigurationOption> ProductConfigurationOptions { get; set; }
        //public DbSet<Store> Stores { get; set; }
        //public DbSet<Promotion> Promotions { get; set; }
        //public DbSet<PromotionProductScope> PromotionProductScopes { get; set; }
        //public DbSet<PromotionCategoryScope> PromotionCategoryScopes { get; set; }
        //public DbSet<Cart> Carts { get; set; }
        //public DbSet<CartItem> CartItems { get; set; }
        //public DbSet<OrderStatus> OrderStatuses { get; set; }
        //public DbSet<PaymentMethod> PaymentMethods { get; set; }
        //public DbSet<DeliveryMethod> DeliveryMethods { get; set; }
        //public DbSet<Order> Orders { get; set; }
        //public DbSet<OrderItem> OrderItems { get; set; }
        //public DbSet<Payment> Payments { get; set; }
        //public DbSet<ContactSubmission> ContactSubmissions { get; set; }

        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //    // Cấu hình các mối quan hệ và các thiết lập khác

        //    // Cấu hình khóa chính cho UserRoles (bảng quan hệ nhiều-nhiều)
        //    modelBuilder.Entity<UserRole>()
        //        .HasKey(ur => new { ur.UserID, ur.RoleID });

        //    // Cấu hình khóa chính cho PromotionProductScope
        //    modelBuilder.Entity<PromotionProductScope>()
        //        .HasKey(pp => new { pp.PromotionID, pp.ProductID });

        //    // Cấu hình khóa chính cho PromotionCategoryScope
        //    modelBuilder.Entity<PromotionCategoryScope>()
        //        .HasKey(pc => new { pc.PromotionID, pc.CategoryID });

        //    // Cấu hình khóa ngoại và các constraint khác có thể thêm vào ở đây

        //    base.OnModelCreating(modelBuilder);
        //}
    }
}
