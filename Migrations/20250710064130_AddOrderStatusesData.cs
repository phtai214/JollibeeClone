using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JollibeeClone.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderStatusesData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Xóa tất cả dữ liệu OrderStatuses hiện tại (nếu có)
            migrationBuilder.Sql("DELETE FROM OrderStatuses");

            // Thêm các trạng thái đơn hàng
            migrationBuilder.InsertData(
                table: "OrderStatuses",
                columns: new[] { "OrderStatusID", "StatusName", "Description" },
                values: new object[,]
                {
                    { 1, "Chờ xác nhận", "Đơn hàng đang chờ được xác nhận từ cửa hàng" },
                    { 2, "Đã xác nhận", "Đơn hàng đã được xác nhận và sẽ được chuẩn bị" },
                    { 3, "Đang chuẩn bị", "Đơn hàng đang được chuẩn bị" },
                    { 4, "Đang giao hàng", "Đơn hàng đang được giao đến địa chỉ của bạn" },
                    { 5, "Sẵn sàng lấy hàng", "Đơn hàng đã sẵn sàng để lấy tại cửa hàng" },
                    { 6, "Hoàn thành", "Đơn hàng đã được hoàn thành thành công" },
                    { 7, "Đã hủy", "Đơn hàng đã bị hủy" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Xóa các trạng thái đơn hàng đã thêm
            migrationBuilder.Sql("DELETE FROM OrderStatuses WHERE OrderStatusID IN (1, 2, 3, 4, 5, 6, 7)");
        }
    }
}
