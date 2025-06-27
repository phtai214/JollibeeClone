using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JollibeeClone.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductConfigurationOptions_ProductVariants_VariantID",
                table: "ProductConfigurationOptions");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductConfigurationOptions_ProductVariants_VariantID",
                table: "ProductConfigurationOptions",
                column: "VariantID",
                principalTable: "ProductVariants",
                principalColumn: "VariantID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductConfigurationOptions_ProductVariants_VariantID",
                table: "ProductConfigurationOptions");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductConfigurationOptions_ProductVariants_VariantID",
                table: "ProductConfigurationOptions",
                column: "VariantID",
                principalTable: "ProductVariants",
                principalColumn: "VariantID",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
