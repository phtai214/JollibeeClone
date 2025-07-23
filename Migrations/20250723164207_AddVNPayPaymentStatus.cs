using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JollibeeClone.Migrations
{
    /// <inheritdoc />
    public partial class AddVNPayPaymentStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Thêm status "Chờ thanh toán" cho VNPay
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM OrderStatuses WHERE StatusName = N'Chờ thanh toán')
                BEGIN
                    INSERT INTO OrderStatuses (StatusName, Description) 
                    VALUES (N'Chờ thanh toán', N'Đơn hàng đã được tạo và đang chờ thanh toán qua VNPay')
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Xóa status "Chờ thanh toán" khi rollback
            migrationBuilder.Sql(@"
                DELETE FROM OrderStatuses WHERE StatusName = N'Chờ thanh toán'
            ");
        }
    }
}
