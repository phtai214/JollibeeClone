using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JollibeeClone.Migrations
{
    /// <inheritdoc />
    public partial class UpdateVariantNamesFormat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update variant names to use proper currency format
            migrationBuilder.Sql(@"
                UPDATE ProductVariants 
                SET VariantName = N'Lớn (+6.000đ)' 
                WHERE VariantName = N'Lớn (+6K)';

                UPDATE ProductVariants 
                SET VariantName = N'Lớn (+8.000đ)' 
                WHERE VariantName = N'Lớn (+8K)';

                UPDATE ProductVariants 
                SET VariantName = N'Lớn (+10.000đ)' 
                WHERE VariantName = N'Lớn (+10K)';

                UPDATE ProductVariants 
                SET VariantName = N'Lớn (+11.000đ)' 
                WHERE VariantName = N'Lớn (+11K)';

                UPDATE ProductVariants 
                SET VariantName = N'Lớn (+12.000đ)' 
                WHERE VariantName = N'Lớn (+12K)';

                UPDATE ProductVariants 
                SET VariantName = N'Lớn (+15.000đ)' 
                WHERE VariantName = N'Lớn (+15K)';

                UPDATE ProductVariants 
                SET VariantName = N'Lớn (+18.000đ)' 
                WHERE VariantName = N'Lớn (+18K)';

                UPDATE ProductVariants 
                SET VariantName = N'Lớn (+20.000đ)' 
                WHERE VariantName = N'Lớn (+20K)';

                UPDATE ProductVariants 
                SET VariantName = N'Lớn (+22.000đ)' 
                WHERE VariantName = N'Lớn (+22K)';

                UPDATE ProductVariants 
                SET VariantName = N'Lớn (+25.000đ)' 
                WHERE VariantName = N'Lớn (+25K)';

                UPDATE ProductVariants 
                SET VariantName = N'Lớn (+30.000đ)' 
                WHERE VariantName = N'Lớn (+30K)';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert back to K format if needed
            migrationBuilder.Sql(@"
                UPDATE ProductVariants 
                SET VariantName = N'Lớn (+6K)' 
                WHERE VariantName = N'Lớn (+6.000đ)';

                UPDATE ProductVariants 
                SET VariantName = N'Lớn (+8K)' 
                WHERE VariantName = N'Lớn (+8.000đ)';

                UPDATE ProductVariants 
                SET VariantName = N'Lớn (+10K)' 
                WHERE VariantName = N'Lớn (+10.000đ)';

                UPDATE ProductVariants 
                SET VariantName = N'Lớn (+11K)' 
                WHERE VariantName = N'Lớn (+11.000đ)';

                UPDATE ProductVariants 
                SET VariantName = N'Lớn (+12K)' 
                WHERE VariantName = N'Lớn (+12.000đ)';

                UPDATE ProductVariants 
                SET VariantName = N'Lớn (+15K)' 
                WHERE VariantName = N'Lớn (+15.000đ)';

                UPDATE ProductVariants 
                SET VariantName = N'Lớn (+18K)' 
                WHERE VariantName = N'Lớn (+18.000đ)';

                UPDATE ProductVariants 
                SET VariantName = N'Lớn (+20K)' 
                WHERE VariantName = N'Lớn (+20.000đ)';

                UPDATE ProductVariants 
                SET VariantName = N'Lớn (+22K)' 
                WHERE VariantName = N'Lớn (+22.000đ)';

                UPDATE ProductVariants 
                SET VariantName = N'Lớn (+25K)' 
                WHERE VariantName = N'Lớn (+25.000đ)';

                UPDATE ProductVariants 
                SET VariantName = N'Lớn (+30K)' 
                WHERE VariantName = N'Lớn (+30.000đ)';
            ");
        }
    }
}
