using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JollibeeClone.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMoMoToVNPAY : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Cập nhật "Ví MoMo" thành "Ví VNPAY"
            migrationBuilder.Sql("UPDATE PaymentMethods SET MethodName = N'Ví VNPAY' WHERE MethodName = N'Ví MoMo'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback: Cập nhật "Ví VNPAY" về "Ví MoMo"
            migrationBuilder.Sql("UPDATE PaymentMethods SET MethodName = N'Ví MoMo' WHERE MethodName = N'Ví VNPAY'");
        }
    }
}
