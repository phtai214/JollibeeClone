using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JollibeeClone.Migrations
{
    /// <inheritdoc />
    public partial class UpdateVariantsToSimpleFormat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Xóa tất cả variants hiện tại
            migrationBuilder.Sql("DELETE FROM ProductVariants");

            // Tạo lại chỉ với 2 variants đơn giản cho tất cả sản phẩm
            migrationBuilder.Sql(@"
                INSERT INTO ProductVariants (ProductID, VariantName, VariantType, PriceAdjustment, IsDefault, IsAvailable, DisplayOrder)
                SELECT p.ProductID, N'Vừa', N'Size', 0.00, 1, 1, 0
                FROM Products p 
                WHERE p.IsAvailable = 1 AND p.IsConfigurable = 0;
            ");

            migrationBuilder.Sql(@"
                INSERT INTO ProductVariants (ProductID, VariantName, VariantType, PriceAdjustment, IsDefault, IsAvailable, DisplayOrder)
                SELECT p.ProductID, N'Lớn', N'Size', 8000.00, 0, 1, 1
                FROM Products p 
                WHERE p.IsAvailable = 1 AND p.IsConfigurable = 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback: xóa variants được tạo
            migrationBuilder.Sql(@"
                DELETE FROM ProductVariants 
                WHERE VariantName IN (N'Vừa', N'Lớn');
            ");
        }
    }
}
