using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JollibeeClone.Migrations
{
    /// <inheritdoc />
    public partial class AddAutoVoucherAndRewardProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoVoucherGenerated",
                table: "Promotions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "RewardThreshold",
                table: "Promotions",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserRewardProgresses",
                columns: table => new
                {
                    UserRewardProgressID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    RewardThreshold = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CurrentSpending = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    VoucherClaimed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    GeneratedPromotionID = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    VoucherClaimedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRewardProgresses", x => x.UserRewardProgressID);
                    table.ForeignKey(
                        name: "FK_UserRewardProgresses_Promotions_GeneratedPromotionID",
                        column: x => x.GeneratedPromotionID,
                        principalTable: "Promotions",
                        principalColumn: "PromotionID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserRewardProgresses_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserRewardProgresses_GeneratedPromotionID",
                table: "UserRewardProgresses",
                column: "GeneratedPromotionID");

            migrationBuilder.CreateIndex(
                name: "IX_UserRewardProgresses_UserID_RewardThreshold",
                table: "UserRewardProgresses",
                columns: new[] { "UserID", "RewardThreshold" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserRewardProgresses");

            migrationBuilder.DropColumn(
                name: "AutoVoucherGenerated",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "RewardThreshold",
                table: "Promotions");
        }
    }
}
