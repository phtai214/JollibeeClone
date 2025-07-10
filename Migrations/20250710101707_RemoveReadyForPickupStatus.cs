using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JollibeeClone.Migrations
{
    /// <inheritdoc />
    public partial class RemoveReadyForPickupStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Cập nhật tất cả đơn hàng có trạng thái "Sẵn sàng lấy hàng" (ID=5) thành "Hoàn thành" (ID=6)
            migrationBuilder.Sql(@"
                UPDATE Orders 
                SET OrderStatusID = 6 
                WHERE OrderStatusID = 5
            ");

            // Xóa trạng thái "Sẵn sàng lấy hàng" (ID=5)
            migrationBuilder.Sql("DELETE FROM OrderStatuses WHERE OrderStatusID = 5");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Thêm lại trạng thái 'Sẵn sàng lấy hàng'
            migrationBuilder.InsertData(
                table: "OrderStatuses",
                columns: new[] { "OrderStatusID", "StatusName", "Description" },
                values: new object[] { 5, "Sẵn sàng lấy hàng", "Đơn hàng đã sẵn sàng để lấy tại cửa hàng" }
            );
        }
    }
}
