using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JollibeeClone.Migrations
{
    /// <inheritdoc />
    public partial class RestoreReadyForPickupStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Thêm lại trạng thái "Sẵn sàng lấy hàng" nếu chưa tồn tại
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM OrderStatuses WHERE OrderStatusID = 5)
                BEGIN
                    SET IDENTITY_INSERT OrderStatuses ON;
                    INSERT INTO OrderStatuses (OrderStatusID, StatusName, Description)
                    VALUES (5, N'Sẵn sàng lấy hàng', N'Đơn hàng đã sẵn sàng để lấy tại cửa hàng');
                    SET IDENTITY_INSERT OrderStatuses OFF;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Xóa trạng thái "Sẵn sàng lấy hàng" nếu có
            migrationBuilder.Sql("DELETE FROM OrderStatuses WHERE OrderStatusID = 5");
        }
    }
}
