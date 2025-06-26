using JollibeeClone.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace JollibeeClone.Data
{
    public static class SeedAdminData
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            try
            {
                // Seed Roles first
                if (!await context.Roles.AnyAsync())
                {
                    var roles = new List<Role>
                    {
                        new Role { RoleName = "Admin" },
                        new Role { RoleName = "Manager" },
                        new Role { RoleName = "User" },
                        new Role { RoleName = "Customer" }
                    };

                    context.Roles.AddRange(roles);
                    await context.SaveChangesAsync();
                    Console.WriteLine("✅ Seeded Roles successfully");
                }

                // Seed Admin User
                var adminEmail = "admin@jollibee.com";
                var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
                
                if (adminUser == null)
                {
                    // Tạo admin user mới
                    adminUser = new User
                    {
                        FullName = "Administrator",
                        Email = adminEmail,
                        PhoneNumber = "0123456789",
                        PasswordHash = HashPassword("admin123"), // Password cố định cho cả team
                        IsActive = true
                    };

                    context.Users.Add(adminUser);
                    await context.SaveChangesAsync();
                    Console.WriteLine("✅ Created Admin User successfully");
                }
                else
                {
                    // Update existing admin password để đồng bộ
                    adminUser.PasswordHash = HashPassword("admin123");
                    await context.SaveChangesAsync();
                    Console.WriteLine("✅ Updated Admin User password");
                }

                // Assign Admin Role
                var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
                if (adminRole != null)
                {
                    var existingUserRole = await context.UserRoles
                        .FirstOrDefaultAsync(ur => ur.UserID == adminUser.UserID && ur.RoleID == adminRole.RoleID);

                    if (existingUserRole == null)
                    {
                        var userRole = new UserRole
                        {
                            UserID = adminUser.UserID,
                            RoleID = adminRole.RoleID
                        };

                        context.UserRoles.Add(userRole);
                        await context.SaveChangesAsync();
                        Console.WriteLine("✅ Assigned Admin Role successfully");
                    }
                }

                // Seed Order Statuses
                if (!await context.OrderStatuses.AnyAsync())
                {
                    var orderStatuses = new List<OrderStatuses>
                    {
                        new OrderStatuses { StatusName = "Chờ xác nhận", Description = "Đơn hàng mới chờ xác nhận" },
                        new OrderStatuses { StatusName = "Đã xác nhận", Description = "Đơn hàng đã được xác nhận" },
                        new OrderStatuses { StatusName = "Đang chuẩn bị", Description = "Đang chuẩn bị đơn hàng" },
                        new OrderStatuses { StatusName = "Đang giao", Description = "Đơn hàng đang được giao" },
                        new OrderStatuses { StatusName = "Đã giao", Description = "Đơn hàng đã được giao thành công" },
                        new OrderStatuses { StatusName = "Đã hủy", Description = "Đơn hàng đã bị hủy" }
                    };

                    context.OrderStatuses.AddRange(orderStatuses);
                    await context.SaveChangesAsync();
                    Console.WriteLine("✅ Seeded Order Statuses successfully");
                }

                // Seed Payment Methods
                if (!await context.PaymentMethods.AnyAsync())
                {
                    var paymentMethods = new List<PaymentMethods>
                    {
                        new PaymentMethods { MethodName = "Tiền mặt", IsActive = true },
                        new PaymentMethods { MethodName = "Chuyển khoản", IsActive = true },
                        new PaymentMethods { MethodName = "Ví điện tử", IsActive = true },
                        new PaymentMethods { MethodName = "Thẻ tín dụng", IsActive = true }
                    };

                    context.PaymentMethods.AddRange(paymentMethods);
                    await context.SaveChangesAsync();
                    Console.WriteLine("✅ Seeded Payment Methods successfully");
                }

                // Seed Delivery Methods
                if (!await context.DeliveryMethods.AnyAsync())
                {
                    var deliveryMethods = new List<DeliveryMethods>
                    {
                        new DeliveryMethods { MethodName = "Giao hàng tận nơi", Description = "Giao hàng tận nơi trong 30-45 phút", IsActive = true },
                        new DeliveryMethods { MethodName = "Nhận tại cửa hàng", Description = "Khách hàng đến nhận tại cửa hàng", IsActive = true }
                    };

                    context.DeliveryMethods.AddRange(deliveryMethods);
                    await context.SaveChangesAsync();
                    Console.WriteLine("✅ Seeded Delivery Methods successfully");
                }

                // Seed Sample Categories
                if (!await context.Categories.AnyAsync())
                {
                    var categories = new List<Categories>
                    {
                        new Categories { CategoryName = "Gà Giòn Vui Vẻ", Description = "Gà rán giòn tan, thơm ngon", DisplayOrder = 1, IsActive = true },
                        new Categories { CategoryName = "Gà Sốt Cay", Description = "Gà rán phủ sốt cay đậm đà", DisplayOrder = 2, IsActive = true },
                        new Categories { CategoryName = "Burger/Cơm", Description = "Burger và cơm đa dạng", DisplayOrder = 3, IsActive = true },
                        new Categories { CategoryName = "Mì Ý Jolly", Description = "Mì Ý phong cách Ý chính thống", DisplayOrder = 4, IsActive = true },
                        new Categories { CategoryName = "Phần Ăn Phụ", Description = "Khoai tây chiên, tôm viên...", DisplayOrder = 5, IsActive = true },
                        new Categories { CategoryName = "Thức Uống", Description = "Đồ uống giải khát đa dạng", DisplayOrder = 6, IsActive = true },
                        new Categories { CategoryName = "Món Tráng Miệng", Description = "Bánh kẹo, kem tráng miệng", DisplayOrder = 7, IsActive = true }
                    };

                    context.Categories.AddRange(categories);
                    await context.SaveChangesAsync();
                    Console.WriteLine("✅ Seeded Categories successfully");
                }

                Console.WriteLine("🎉 All seed data completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error seeding data: {ex.Message}");
                throw;
            }
        }

        private static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        public static async Task ResetAdminPasswordAsync(AppDbContext context)
        {
            try
            {
                var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "admin@jollibee.com");
                if (adminUser != null)
                {
                    adminUser.PasswordHash = HashPassword("admin123");
                    await context.SaveChangesAsync();
                    Console.WriteLine("✅ Admin password reset to 'admin123'");
                }
                else
                {
                    Console.WriteLine("❌ Admin user not found");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error resetting admin password: {ex.Message}");
                throw;
            }
        }
    }
} 