using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JollibeeClone.Migrations
{
    /// <inheritdoc />
    public partial class AddProductVariantAndComboFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ThumbnailUrl",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomImageUrl",
                table: "ProductConfigurationOptions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "ProductConfigurationOptions",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "VariantID",
                table: "ProductConfigurationOptions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductVariants",
                columns: table => new
                {
                    VariantID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    VariantName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VariantType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PriceAdjustment = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0.00m),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariants", x => x.VariantID);
                    table.ForeignKey(
                        name: "FK_ProductVariants_Products_ProductID",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductConfigurationOptions_VariantID",
                table: "ProductConfigurationOptions",
                column: "VariantID");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProductConfigurationOption_Quantity",
                table: "ProductConfigurationOptions",
                sql: "Quantity > 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductID",
                table: "ProductVariants",
                column: "ProductID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductConfigurationOptions_ProductVariants_VariantID",
                table: "ProductConfigurationOptions",
                column: "VariantID",
                principalTable: "ProductVariants",
                principalColumn: "VariantID",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductConfigurationOptions_ProductVariants_VariantID",
                table: "ProductConfigurationOptions");

            migrationBuilder.DropTable(
                name: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_ProductConfigurationOptions_VariantID",
                table: "ProductConfigurationOptions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ProductConfigurationOption_Quantity",
                table: "ProductConfigurationOptions");

            migrationBuilder.DropColumn(
                name: "ThumbnailUrl",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CustomImageUrl",
                table: "ProductConfigurationOptions");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "ProductConfigurationOptions");

            migrationBuilder.DropColumn(
                name: "VariantID",
                table: "ProductConfigurationOptions");
        }
    }
}
