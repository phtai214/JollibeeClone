using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JollibeeClone.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPromotionTableOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserPromotions",
                columns: table => new
                {
                    UserPromotionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    PromotionID = table.Column<int>(type: "int", nullable: false),
                    UsedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OrderID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPromotions", x => x.UserPromotionID);
                    table.ForeignKey(
                        name: "FK_UserPromotions_Orders_OrderID",
                        column: x => x.OrderID,
                        principalTable: "Orders",
                        principalColumn: "OrderID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserPromotions_Promotions_PromotionID",
                        column: x => x.PromotionID,
                        principalTable: "Promotions",
                        principalColumn: "PromotionID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserPromotions_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserPromotions_OrderID",
                table: "UserPromotions",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_UserPromotions_PromotionID",
                table: "UserPromotions",
                column: "PromotionID");

            migrationBuilder.CreateIndex(
                name: "IX_UserPromotions_UserID_PromotionID",
                table: "UserPromotions",
                columns: new[] { "UserID", "PromotionID" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPromotions");
        }
    }
}

