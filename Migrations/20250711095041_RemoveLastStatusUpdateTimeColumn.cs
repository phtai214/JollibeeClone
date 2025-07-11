using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JollibeeClone.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLastStatusUpdateTimeColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, migrate existing order data to OrderStatusHistory
            // This will create history records for existing orders
            migrationBuilder.Sql(@"
                -- Insert initial status (Đặt hàng thành công) for all existing orders
                INSERT INTO OrderStatusHistories (OrderID, OrderStatusID, UpdatedAt, UpdatedBy, Note)
                SELECT 
                    OrderID,
                    1, -- Chờ xác nhận status
                    OrderDate,
                    'System',
                    'Đơn hàng được tạo (migrated data)'
                FROM Orders
                WHERE OrderID NOT IN (SELECT DISTINCT OrderID FROM OrderStatusHistories)
            ");

            // Insert current status for orders that are not in 'Chờ xác nhận' status
            migrationBuilder.Sql(@"
                -- Insert current status for orders that have progressed beyond initial status
                INSERT INTO OrderStatusHistories (OrderID, OrderStatusID, UpdatedAt, UpdatedBy, Note)
                SELECT 
                    o.OrderID,
                    o.OrderStatusID,
                    COALESCE(o.LastStatusUpdateTime, DATEADD(MINUTE, 10, o.OrderDate)),
                    'System',
                    'Trạng thái hiện tại (migrated data)'
                FROM Orders o
                WHERE o.OrderStatusID != 1 
                AND NOT EXISTS (
                    SELECT 1 FROM OrderStatusHistories h 
                    WHERE h.OrderID = o.OrderID AND h.OrderStatusID = o.OrderStatusID
                )
            ");

            // Now remove the LastStatusUpdateTime column
            migrationBuilder.DropColumn(
                name: "LastStatusUpdateTime",
                table: "Orders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastStatusUpdateTime",
                table: "Orders",
                type: "datetime2",
                nullable: true);
        }
    }
}
