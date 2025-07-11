using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JollibeeClone.Migrations
{
    /// <inheritdoc />
    public partial class UpdateExistingOrdersLastStatusUpdateTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Cập nhật LastStatusUpdateTime cho các đơn hàng hiện có dựa trên trạng thái
            migrationBuilder.Sql(@"
                UPDATE Orders 
                SET LastStatusUpdateTime = 
                    CASE 
                        WHEN OrderStatusID = 1 THEN OrderDate -- Chờ xác nhận
                        WHEN OrderStatusID = 2 THEN DATEADD(MINUTE, 5, OrderDate) -- Đã xác nhận
                        WHEN OrderStatusID = 3 THEN DATEADD(MINUTE, 15, OrderDate) -- Đang chuẩn bị
                        WHEN OrderStatusID = 4 THEN DATEADD(MINUTE, 30, OrderDate) -- Đang giao hàng
                        WHEN OrderStatusID = 5 THEN DATEADD(MINUTE, 25, OrderDate) -- Sẵn sàng lấy hàng
                        WHEN OrderStatusID = 6 THEN DATEADD(MINUTE, 45, OrderDate) -- Hoàn thành
                        WHEN OrderStatusID = 7 THEN DATEADD(MINUTE, 10, OrderDate) -- Đã hủy
                        ELSE OrderDate
                    END
                WHERE LastStatusUpdateTime = OrderDate -- Chỉ update những đơn chưa được cập nhật thủ công
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback: Set all LastStatusUpdateTime back to OrderDate
            migrationBuilder.Sql(@"
                UPDATE Orders 
                SET LastStatusUpdateTime = OrderDate
            ");
        }
    }
}
