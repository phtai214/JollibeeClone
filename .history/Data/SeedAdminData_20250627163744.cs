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

        public static async Task SeedProductVariantsAsync(AppDbContext context)
        {
            // Kiểm tra xem đã có biến thể chưa
            if (await context.ProductVariants.AnyAsync())
            {
                return; // Đã có dữ liệu rồi
            }

            var products = await context.Products.ToListAsync();
            var variants = new List<ProductVariant>();

            foreach (var product in products)
            {
                // Thêm biến thể size cho tất cả sản phẩm
                variants.AddRange(new[]
                {
                    new ProductVariant
                    {
                        ProductID = product.ProductID,
                        VariantName = "vừa",
                        VariantType = "Size",
                        PriceAdjustment = 0.00m,
                        IsDefault = true,
                        IsAvailable = true,
                        DisplayOrder = 1
                    },
                    new ProductVariant
                    {
                        ProductID = product.ProductID,
                        VariantName = "Size lớn",
                        VariantType = "Size",
                        PriceAdjustment = 10000.00m,
                        IsDefault = false,
                        IsAvailable = true,
                        DisplayOrder = 2
                    }
                });

                // Nếu là đồ uống, thêm biến thể đá
                if (product.CategoryID == 5) // Assuming category 5 is drinks
                {
                    variants.AddRange(new[]
                    {
                        new ProductVariant
                        {
                            ProductID = product.ProductID,
                            VariantName = "Ít đá",
                            VariantType = "Đá",
                            PriceAdjustment = 0.00m,
                            IsDefault = true,
                            IsAvailable = true,
                            DisplayOrder = 3
                        },
                        new ProductVariant
                        {
                            ProductID = product.ProductID,
                            VariantName = "Nhiều đá",
                            VariantType = "Đá",
                            PriceAdjustment = 0.00m,
                            IsDefault = false,
                            IsAvailable = true,
                            DisplayOrder = 4
                        },
                        new ProductVariant
                        {
                            ProductID = product.ProductID,
                            VariantName = "Không đá",
                            VariantType = "Đá",
                            PriceAdjustment = 0.00m,
                            IsDefault = false,
                            IsAvailable = true,
                            DisplayOrder = 5
                        }
                    });
                }

                // Nếu là gà, thêm biến thể cay
                if (product.ProductName.ToLower().Contains("gà") || product.ProductName.ToLower().Contains("chicken"))
                {
                    variants.AddRange(new[]
                    {
                        new ProductVariant
                        {
                            ProductID = product.ProductID,
                            VariantName = "Không cay",
                            VariantType = "Độ cay",
                            PriceAdjustment = 0.00m,
                            IsDefault = true,
                            IsAvailable = true,
                            DisplayOrder = 6
                        },
                        new ProductVariant
                        {
                            ProductID = product.ProductID,
                            VariantName = "Cay vừa",
                            VariantType = "Độ cay",
                            PriceAdjustment = 0.00m,
                            IsDefault = false,
                            IsAvailable = true,
                            DisplayOrder = 7
                        },
                        new ProductVariant
                        {
                            ProductID = product.ProductID,
                            VariantName = "Cay nhiều",
                            VariantType = "Độ cay",
                            PriceAdjustment = 5000.00m,
                            IsDefault = false,
                            IsAvailable = true,
                            DisplayOrder = 8
                        }
                    });
                }
            }

            context.ProductVariants.AddRange(variants);
            await context.SaveChangesAsync();
        }

        // Phương thức chạy SQL trực tiếp để tạo biến thể mẫu
        public static async Task SeedProductVariantsDirectAsync(AppDbContext context)
        {
            // Kiểm tra xem đã có biến thể chưa
            if (await context.ProductVariants.AnyAsync())
            {
                return;
            }

            // Tạo biến thể cho tất cả sản phẩm bằng SQL
            var sql = @"
                INSERT INTO ProductVariants (ProductID, VariantName, VariantType, PriceAdjustment, IsDefault, IsAvailable, DisplayOrder)
                SELECT 
                    p.ProductID,
                    'Size vừa' as VariantName,
                    'Size' as VariantType,
                    0.00 as PriceAdjustment,
                    1 as IsDefault,
                    1 as IsAvailable,
                    1 as DisplayOrder
                FROM Products p
                WHERE p.IsAvailable = 1

                UNION ALL

                SELECT 
                    p.ProductID,
                    'Size lớn' as VariantName,
                    'Size' as VariantType,
                    10000.00 as PriceAdjustment,
                    0 as IsDefault,
                    1 as IsAvailable,
                    2 as DisplayOrder
                FROM Products p
                WHERE p.IsAvailable = 1

                UNION ALL

                SELECT 
                    p.ProductID,
                    'Thêm phô mai' as VariantName,
                    'Topping' as VariantType,
                    5000.00 as PriceAdjustment,
                    0 as IsDefault,
                    1 as IsAvailable,
                    3 as DisplayOrder
                FROM Products p
                WHERE p.IsAvailable = 1 AND (LOWER(p.ProductName) LIKE '%burger%' OR LOWER(p.ProductName) LIKE '%bánh%')
            ";

            await context.Database.ExecuteSqlRawAsync(sql);
        }
    }
} 