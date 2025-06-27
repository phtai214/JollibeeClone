using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JollibeeClone.Migrations
{
    /// <inheritdoc />
    public partial class AddVariantsForAllProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Thêm variants mặc định cho tất cả sản phẩm chưa có variants
            migrationBuilder.Sql(@"
                INSERT INTO ProductVariants (ProductID, VariantName, VariantType, PriceAdjustment, IsDefault, IsAvailable, DisplayOrder)
                SELECT DISTINCT p.ProductID, N'Vừa', N'Size', 0.00, 1, 1, 0
                FROM Products p 
                WHERE p.IsAvailable = 1 
                AND p.IsConfigurable = 0 
                AND NOT EXISTS (
                    SELECT 1 FROM ProductVariants pv 
                    WHERE pv.ProductID = p.ProductID
                );
            ");

            // Thêm variants size lớn cho tất cả sản phẩm
            migrationBuilder.Sql(@"
                INSERT INTO ProductVariants (ProductID, VariantName, VariantType, PriceAdjustment, IsDefault, IsAvailable, DisplayOrder)
                SELECT DISTINCT p.ProductID, N'Lớn (+8.000đ)', N'Size', 8000.00, 0, 1, 1
                FROM Products p 
                WHERE p.IsAvailable = 1 
                AND p.IsConfigurable = 0 
                AND NOT EXISTS (
                    SELECT 1 FROM ProductVariants pv 
                    WHERE pv.ProductID = p.ProductID 
                    AND pv.VariantName = N'Lớn (+8.000đ)'
                );
            ");

            // Thêm variants topping cho sản phẩm có "burger", "bánh", "mì" trong tên
            migrationBuilder.Sql(@"
                INSERT INTO ProductVariants (ProductID, VariantName, VariantType, PriceAdjustment, IsDefault, IsAvailable, DisplayOrder)
                SELECT DISTINCT p.ProductID, N'Thêm phô mai (+5.000đ)', N'Topping', 5000.00, 0, 1, 2
                FROM Products p 
                WHERE p.IsAvailable = 1 
                AND p.IsConfigurable = 0 
                AND (LOWER(p.ProductName) LIKE N'%burger%' 
                     OR LOWER(p.ProductName) LIKE N'%bánh%' 
                     OR LOWER(p.ProductName) LIKE N'%mì%'
                     OR LOWER(p.ProductName) LIKE N'%mi%')
                AND NOT EXISTS (
                    SELECT 1 FROM ProductVariants pv 
                    WHERE pv.ProductID = p.ProductID 
                    AND pv.VariantName = N'Thêm phô mai (+5.000đ)'
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Xóa tất cả variants được tạo bởi migration này
            migrationBuilder.Sql(@"
                DELETE FROM ProductVariants 
                WHERE VariantName IN (N'Vừa', N'Lớn (+8.000đ)', N'Thêm phô mai (+5.000đ)');
            ");
        }
    }
}
