using Microsoft.EntityFrameworkCore;
using JollibeeClone.Models;
using System.Data;
using JollibeeClone.Areas.Admin.Models;
namespace JollibeeClone.Areas.Admin.Data
{
    public class AppDbContext:DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }
        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<UserAddress> UserAddresses { get; set; }
        public DbSet<Categories> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductConfigurationGroup> ProductConfigurationGroups { get; set; }
        public DbSet<ProductConfigurationOption> ProductConfigurationOptions { get; set; }
        public DbSet<Store> Stores { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<PromotionProductScope> PromotionProductScopes { get; set; }
        public DbSet<PromotionCategoryScope> PromotionCategoryScopes { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<OrderStatuses> OrderStatuses { get; set; }
        public DbSet<PaymentMethods> PaymentMethods { get; set; }
        public DbSet<DeliveryMethods> DeliveryMethods { get; set; }
        public DbSet<Orders> Orders { get; set; }
        public DbSet<OrderItems> OrderItems { get; set; }
        public DbSet<Payments> Payments { get; set; }
        public DbSet<ContactSubmissions> ContactSubmissions { get; set; }
        public DbSet<UserPromotion> UserPromotions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Role entity
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.RoleID);
                entity.Property(e => e.RoleID).UseIdentityColumn(1, 1);
                entity.HasIndex(e => e.RoleName).IsUnique();
                entity.Property(e => e.RoleName).HasMaxLength(50).IsRequired();
            });

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserID);
                entity.Property(e => e.UserID).UseIdentityColumn(1, 1);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.FullName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });

            // Configure UserRole entity (Many-to-Many relationship)
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasKey(e => new { e.UserID, e.RoleID });

                entity.HasOne(e => e.User)
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(e => e.UserID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(e => e.RoleID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure UserAddress entity
            modelBuilder.Entity<UserAddress>(entity =>
            {
                entity.HasKey(e => e.AddressID);
                entity.Property(e => e.AddressID).UseIdentityColumn(1, 1);
                entity.Property(e => e.FullName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.PhoneNumber).HasMaxLength(20).IsRequired();
                entity.Property(e => e.Address).HasMaxLength(255);
                entity.Property(e => e.Note).HasMaxLength(255);
                entity.Property(e => e.IsDefault).HasDefaultValue(false);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.UserAddresses)
                    .HasForeignKey(e => e.UserID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Category entity
            modelBuilder.Entity<Categories>(entity =>
            {
                entity.HasKey(e => e.CategoryID);
                entity.Property(e => e.CategoryID).UseIdentityColumn(1, 1);
                entity.HasIndex(e => e.CategoryName).IsUnique();
                entity.Property(e => e.CategoryName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
                entity.Property(e => e.IsActive).HasDefaultValue(true);

                // Self-referencing relationship
                entity.HasOne(e => e.ParentCategory)
                    .WithMany(c => c.SubCategories)
                    .HasForeignKey(e => e.ParentCategoryID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Product entity
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.ProductID);
                entity.Property(e => e.ProductID).UseIdentityColumn(1, 1);
                entity.Property(e => e.ProductName).HasMaxLength(200).IsRequired();
                entity.Property(e => e.ShortDescription).HasMaxLength(500);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.OriginalPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.IsConfigurable).HasDefaultValue(false);
                entity.Property(e => e.IsAvailable).HasDefaultValue(true);

                entity.HasOne(e => e.Category)
                    .WithMany(c => c.Products)
                    .HasForeignKey(e => e.CategoryID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure ProductConfigurationGroup entity
            modelBuilder.Entity<ProductConfigurationGroup>(entity =>
            {
                entity.HasKey(e => e.ConfigGroupID);
                entity.Property(e => e.ConfigGroupID).UseIdentityColumn(1, 1);
                entity.Property(e => e.GroupName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.MinSelections).HasDefaultValue(1);
                entity.Property(e => e.MaxSelections).HasDefaultValue(1);
                entity.Property(e => e.DisplayOrder).HasDefaultValue(0);

                entity.HasOne(e => e.MainProduct)
                    .WithMany(p => p.ProductConfigurationGroups)
                    .HasForeignKey(e => e.MainProductID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure ProductConfigurationOption entity
            modelBuilder.Entity<ProductConfigurationOption>(entity =>
            {
                entity.HasKey(e => e.ConfigOptionID);
                entity.Property(e => e.ConfigOptionID).UseIdentityColumn(1, 1);
                entity.Property(e => e.PriceAdjustment).HasColumnType("decimal(18,2)").HasDefaultValue(0.00m);
                entity.Property(e => e.IsDefault).HasDefaultValue(false);
                entity.Property(e => e.DisplayOrder).HasDefaultValue(0);

                entity.HasOne(e => e.ConfigGroup)
                    .WithMany(g => g.ProductConfigurationOptions)
                    .HasForeignKey(e => e.ConfigGroupID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.OptionProduct)
                    .WithMany(p => p.ProductConfigurationOptions)
                    .HasForeignKey(e => e.OptionProductID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Store entity
            modelBuilder.Entity<Store>(entity =>
            {
                entity.HasKey(e => e.StoreID);
                entity.Property(e => e.StoreID).UseIdentityColumn(1, 1);
                entity.Property(e => e.StoreName).HasMaxLength(150).IsRequired();
                entity.Property(e => e.StreetAddress).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Ward).HasMaxLength(100);
                entity.Property(e => e.District).HasMaxLength(100).IsRequired();
                entity.Property(e => e.City).HasMaxLength(100).IsRequired();
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                entity.Property(e => e.OpeningHours).HasMaxLength(255);
                entity.Property(e => e.Latitude).HasColumnType("decimal(9,6)");
                entity.Property(e => e.Longitude).HasColumnType("decimal(9,6)");
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });

            // Configure Promotion entity
            modelBuilder.Entity<Promotion>(entity =>
            {
                entity.HasKey(e => e.PromotionID);
                entity.Property(e => e.PromotionID).UseIdentityColumn(1, 1);
                entity.Property(e => e.PromotionName).HasMaxLength(150).IsRequired();
                entity.HasIndex(e => e.CouponCode).IsUnique();
                entity.Property(e => e.CouponCode).HasMaxLength(50);
                entity.Property(e => e.DiscountType).HasMaxLength(20).IsRequired();
                entity.Property(e => e.DiscountValue).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.MinOrderValue).HasColumnType("decimal(18,2)");
                entity.Property(e => e.UsesCount).HasDefaultValue(0);
                entity.Property(e => e.IsActive).HasDefaultValue(true);

                // Add check constraint for DiscountType
                entity.HasCheckConstraint("CK_DiscountType_Promotions", "DiscountType IN ('Percentage')");
            });

            // Configure PromotionProductScope entity (Many-to-Many)
            modelBuilder.Entity<PromotionProductScope>(entity =>
            {
                entity.HasKey(e => new { e.PromotionID, e.ProductID });
                entity.ToTable("PromotionProductScope");

                entity.HasOne(e => e.Promotion)
                    .WithMany(p => p.PromotionProductScopes)
                    .HasForeignKey(e => e.PromotionID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Product)
                    .WithMany(p => p.PromotionProductScopes)
                    .HasForeignKey(e => e.ProductID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure PromotionCategoryScope entity (Many-to-Many)
            modelBuilder.Entity<PromotionCategoryScope>(entity =>
            {
                entity.HasKey(e => new { e.PromotionID, e.CategoryID });
                entity.ToTable("PromotionCategoryScope");

                entity.HasOne(e => e.Promotion)
                    .WithMany(p => p.PromotionCategoryScopes)
                    .HasForeignKey(e => e.PromotionID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Category)
                    .WithMany(c => c.PromotionCategoryScopes)
                    .HasForeignKey(e => e.CategoryID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Cart entity
            modelBuilder.Entity<Cart>(entity =>
            {
                entity.HasKey(e => e.CartID);
                entity.Property(e => e.CartID).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.SessionID).HasMaxLength(255);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.LastUpdatedDate).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Carts)
                    .HasForeignKey(e => e.UserID)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure CartItem entity
            modelBuilder.Entity<CartItem>(entity =>
            {
                entity.HasKey(e => e.CartItemID);
                entity.Property(e => e.CartItemID).UseIdentityColumn(1, 1);
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.DateAdded).HasDefaultValueSql("GETDATE()");

                // Add check constraint for Quantity
                entity.HasCheckConstraint("CK_CartItem_Quantity", "Quantity > 0");

                entity.HasOne(e => e.Cart)
                    .WithMany(c => c.CartItems)
                    .HasForeignKey(e => e.CartID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Product)
                    .WithMany(p => p.CartItems)
                    .HasForeignKey(e => e.ProductID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure OrderStatus entity
            modelBuilder.Entity<OrderStatuses>(entity =>
            {
                entity.HasKey(e => e.OrderStatusID);
                entity.Property(e => e.OrderStatusID).UseIdentityColumn(1, 1);
                entity.HasIndex(e => e.StatusName).IsUnique();
                entity.Property(e => e.StatusName).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(255);
            });

            // Configure PaymentMethod entity
            modelBuilder.Entity<PaymentMethods>(entity =>
            {
                entity.HasKey(e => e.PaymentMethodID);
                entity.Property(e => e.PaymentMethodID).UseIdentityColumn(1, 1);
                entity.HasIndex(e => e.MethodName).IsUnique();
                entity.Property(e => e.MethodName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });

            // Configure DeliveryMethod entity
            modelBuilder.Entity<DeliveryMethods>(entity =>
            {
                entity.HasKey(e => e.DeliveryMethodID);
                entity.Property(e => e.DeliveryMethodID).UseIdentityColumn(1, 1);
                entity.HasIndex(e => e.MethodName).IsUnique();
                entity.Property(e => e.MethodName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(255);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });

            // Configure Order entity
            modelBuilder.Entity<Orders>(entity =>
            {
                entity.HasKey(e => e.OrderID);
                entity.Property(e => e.OrderID).UseIdentityColumn(1, 1);
                entity.HasIndex(e => e.OrderCode).IsUnique();
                entity.Property(e => e.OrderCode).HasMaxLength(20).IsRequired();
                entity.Property(e => e.CustomerFullName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.CustomerEmail).HasMaxLength(255).IsRequired();
                entity.Property(e => e.CustomerPhoneNumber).HasMaxLength(20).IsRequired();
                entity.Property(e => e.OrderDate).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.SubtotalAmount).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.ShippingFee).HasColumnType("decimal(18,2)").HasDefaultValue(0);
                entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18,2)").HasDefaultValue(0);
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)").IsRequired();

                // Configure relationships
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Orders)
                    .HasForeignKey(e => e.UserID)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.UserAddress)
                    .WithMany(ua => ua.Orders)
                    .HasForeignKey(e => e.UserAddressID)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.DeliveryMethod)
                    .WithMany(dm => dm.Orders)
                    .HasForeignKey(e => e.DeliveryMethodID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Store)
                    .WithMany(s => s.Orders)
                    .HasForeignKey(e => e.StoreID)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.OrderStatus)
                    .WithMany(os => os.Orders)
                    .HasForeignKey(e => e.OrderStatusID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.PaymentMethod)
                    .WithMany(pm => pm.Orders)
                    .HasForeignKey(e => e.PaymentMethodID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Promotion)
                    .WithMany(p => p.Orders)
                    .HasForeignKey(e => e.PromotionID)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure OrderItem entity
            modelBuilder.Entity<OrderItems>(entity =>
            {
                entity.HasKey(e => e.OrderItemID);
                entity.Property(e => e.OrderItemID).UseIdentityColumn(1, 1);
                entity.Property(e => e.ProductNameSnapshot).HasMaxLength(200).IsRequired();
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.Subtotal).HasColumnType("decimal(18,2)").IsRequired();

                // Add check constraint for Quantity
                entity.HasCheckConstraint("CK_OrderItem_Quantity", "Quantity > 0");

                entity.HasOne(e => e.Order)
                    .WithMany(o => o.OrderItems)
                    .HasForeignKey(e => e.OrderID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Product)
                    .WithMany(p => p.OrderItems)
                    .HasForeignKey(e => e.ProductID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Payment entity
            modelBuilder.Entity<Payments>(entity =>
            {
                entity.HasKey(e => e.PaymentID);
                entity.Property(e => e.PaymentID).UseIdentityColumn(1, 1);
                entity.Property(e => e.PaymentDate).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.TransactionCode).HasMaxLength(255);
                entity.Property(e => e.PaymentStatus).HasMaxLength(50).IsRequired();

                entity.HasOne(e => e.Order)
                    .WithMany(o => o.Payments)
                    .HasForeignKey(e => e.OrderID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.PaymentMethod)
                    .WithMany(pm => pm.Payments)
                    .HasForeignKey(e => e.PaymentMethodID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure ContactSubmission entity
            modelBuilder.Entity<ContactSubmissions>(entity =>
            {
                entity.HasKey(e => e.SubmissionID);
                entity.Property(e => e.SubmissionID).UseIdentityColumn(1, 1);
                entity.Property(e => e.FullName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.PhoneNumber).HasMaxLength(20).IsRequired();
                entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Message).IsRequired();
                entity.Property(e => e.SubmissionDate).HasDefaultValueSql("GETDATE()");
            });

            // Seed initial data
            //SeedData(modelBuilder);
        }

    }
}

