using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JollibeeClone.Migrations
{
    /// <inheritdoc />
    public partial class FixVietnameseEncoding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Xóa tất cả ProductVariants để insert lại với UTF-8 đúng
            migrationBuilder.Sql("DELETE FROM ProductVariants");
            
            // Set database collation cho tiếng Việt
            migrationBuilder.Sql("ALTER DATABASE [Jollibee_DB] COLLATE Vietnamese_CI_AS");
            
            // Insert lại data với UTF-8 đúng
            migrationBuilder.Sql(@"
                INSERT INTO ProductVariants (ProductID, VariantName, VariantType, PriceAdjustment, IsDefault, IsAvailable, DisplayOrder)
                SELECT p.ProductID, N'vừa', N'Size', 0.00, 1, 1, 1
                FROM Products p WHERE p.IsAvailable = 1
            ");
            
            migrationBuilder.Sql(@"
                INSERT INTO ProductVariants (ProductID, VariantName, VariantType, PriceAdjustment, IsDefault, IsAvailable, DisplayOrder)
                SELECT p.ProductID, N'lớn', N'Size', 10000.00, 0, 1, 2
                FROM Products p WHERE p.IsAvailable = 1
            ");
            
            migrationBuilder.Sql(@"
                INSERT INTO ProductVariants (ProductID, VariantName, VariantType, PriceAdjustment, IsDefault, IsAvailable, DisplayOrder)
                SELECT p.ProductID, N'Thêm phô mai', N'Topping', 5000.00, 0, 1, 3
                FROM Products p WHERE p.IsAvailable = 1 AND (LOWER(p.ProductName) LIKE N'%burger%' OR LOWER(p.ProductName) LIKE N'%bánh%')
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert back
            migrationBuilder.Sql("DELETE FROM ProductVariants");
        }
    }
}
