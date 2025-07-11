using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JollibeeClone.Migrations
{
    /// <inheritdoc />
    public partial class AddLastStatusUpdateTimeToOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastStatusUpdateTime",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            // Khởi tạo LastStatusUpdateTime = OrderDate cho các đơn hàng hiện có
            migrationBuilder.Sql(@"
                UPDATE Orders 
                SET LastStatusUpdateTime = OrderDate 
                WHERE LastStatusUpdateTime IS NULL
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastStatusUpdateTime",
                table: "Orders");
        }
    }
}
