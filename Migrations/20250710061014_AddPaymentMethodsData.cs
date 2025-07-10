using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JollibeeClone.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentMethodsData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Xóa tất cả dữ liệu PaymentMethods hiện tại (nếu có)
            migrationBuilder.Sql("DELETE FROM PaymentMethods");

            // Thêm 3 phương thức thanh toán mới
            migrationBuilder.InsertData(
                table: "PaymentMethods",
                columns: new[] { "PaymentMethodID", "MethodName", "IsActive" },
                values: new object[,]
                {
                    { 1, "Tiền mặt", true },
                    { 2, "Ví MoMo", true },
                    { 3, "Ví điện tử", true }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Xóa dữ liệu PaymentMethods khi rollback
            migrationBuilder.Sql("DELETE FROM PaymentMethods WHERE PaymentMethodID IN (1, 2, 3)");
        }
    }
}
