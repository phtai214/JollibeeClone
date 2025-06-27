using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JollibeeClone.Migrations
{
    /// <inheritdoc />
    public partial class AddSizeVariantsData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Thêm các biến thể size lớn cho sản phẩm Pepsi
            migrationBuilder.Sql(@"
                DECLARE @PepsiProductId INT;
                SELECT @PepsiProductId = ProductID FROM Products WHERE ProductName LIKE '%PEPSI%' AND IsConfigurable = 0;
                
                IF @PepsiProductId IS NOT NULL
                BEGIN
                    -- Clear references in ProductConfigurationOptions first
                    UPDATE ProductConfigurationOptions 
                    SET VariantID = NULL 
                    WHERE VariantID IN (SELECT VariantID FROM ProductVariants WHERE ProductID = @PepsiProductId);
                    
                    -- Clear existing variants for Pepsi
                    DELETE FROM ProductVariants WHERE ProductID = @PepsiProductId;
                    
                    -- Insert new size variants
                    INSERT INTO ProductVariants (ProductID, VariantName, VariantType, PriceAdjustment, IsDefault, IsAvailable, DisplayOrder)
                    VALUES 
                        (@PepsiProductId, N'Vừa', N'Size', 0.00, 1, 1, 0),
                        (@PepsiProductId, N'Lớn (+6K)', N'Size', 6000.00, 0, 1, 1),
                        (@PepsiProductId, N'Lớn (+11K)', N'Size', 11000.00, 0, 1, 2),
                        (@PepsiProductId, N'Lớn (+15K)', N'Size', 15000.00, 0, 1, 3),
                        (@PepsiProductId, N'Lớn (+20K)', N'Size', 20000.00, 0, 1, 4),
                        (@PepsiProductId, N'Lớn (+25K)', N'Size', 25000.00, 0, 1, 5),
                        (@PepsiProductId, N'Lớn (+30K)', N'Size', 30000.00, 0, 1, 6);
                END
            ");

            // Thêm biến thể cho các sản phẩm đồ uống khác
            migrationBuilder.Sql(@"
                -- Thêm size variants cho các sản phẩm nước khác
                DECLARE @ProductId INT;
                DECLARE product_cursor CURSOR FOR 
                SELECT ProductID FROM Products 
                WHERE (ProductName LIKE N'%nước%' OR ProductName LIKE N'%coca%' OR ProductName LIKE N'%7up%' OR ProductName LIKE N'%trà%' OR ProductName LIKE N'%cà phê%') 
                AND IsConfigurable = 0 AND ProductID NOT IN (SELECT ProductID FROM Products WHERE ProductName LIKE '%PEPSI%');

                OPEN product_cursor;
                FETCH NEXT FROM product_cursor INTO @ProductId;

                WHILE @@FETCH_STATUS = 0
                BEGIN
                    -- Clear existing variants
                    DELETE FROM ProductVariants WHERE ProductID = @ProductId;
                    
                    -- Insert size variants
                    INSERT INTO ProductVariants (ProductID, VariantName, VariantType, PriceAdjustment, IsDefault, IsAvailable, DisplayOrder)
                    VALUES 
                        (@ProductId, N'Vừa', N'Size', 0.00, 1, 1, 0),
                        (@ProductId, N'Lớn (+8K)', N'Size', 8000.00, 0, 1, 1),
                        (@ProductId, N'Lớn (+12K)', N'Size', 12000.00, 0, 1, 2),
                        (@ProductId, N'Lớn (+18K)', N'Size', 18000.00, 0, 1, 3);

                    FETCH NEXT FROM product_cursor INTO @ProductId;
                END

                CLOSE product_cursor;
                DEALLOCATE product_cursor;
            ");

            // Thêm biến thể cho khoai tây chiên
            migrationBuilder.Sql(@"
                DECLARE @FriesId INT;
                SELECT @FriesId = ProductID FROM Products WHERE ProductName LIKE N'%khoai%' AND IsConfigurable = 0;
                
                IF @FriesId IS NOT NULL
                BEGIN
                    -- Clear existing variants
                    DELETE FROM ProductVariants WHERE ProductID = @FriesId;
                    
                    -- Insert size variants
                    INSERT INTO ProductVariants (ProductID, VariantName, VariantType, PriceAdjustment, IsDefault, IsAvailable, DisplayOrder)
                    VALUES 
                        (@FriesId, N'Vừa', N'Size', 0.00, 1, 1, 0),
                        (@FriesId, N'Lớn (+10K)', N'Size', 10000.00, 0, 1, 1),
                        (@FriesId, N'Lớn (+15K)', N'Size', 15000.00, 0, 1, 2),
                        (@FriesId, N'Lớn (+22K)', N'Size', 22000.00, 0, 1, 3);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove all the added variants
            migrationBuilder.Sql(@"
                DELETE FROM ProductVariants 
                WHERE VariantType = N'Size' 
                AND VariantName IN (
                    N'Vừa', N'Lớn (+6K)', N'Lớn (+8K)', N'Lớn (+10K)', N'Lớn (+11K)', 
                    N'Lớn (+12K)', N'Lớn (+15K)', N'Lớn (+18K)', N'Lớn (+20K)', 
                    N'Lớn (+22K)', N'Lớn (+25K)', N'Lớn (+30K)'
                );
            ");
        }
    }
}
