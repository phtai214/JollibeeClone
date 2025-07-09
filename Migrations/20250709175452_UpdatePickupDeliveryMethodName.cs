using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JollibeeClone.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePickupDeliveryMethodName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update pickup delivery method name to "Hẹn lấy tại cửa hàng"
            migrationBuilder.Sql(@"
                UPDATE DeliveryMethods 
                SET MethodName = N'Hẹn lấy tại cửa hàng',
                    Description = N'Khách hàng đến lấy hàng tại cửa hàng theo thời gian hẹn trước'
                WHERE DeliveryMethodID = 2
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback to original name
            migrationBuilder.Sql(@"
                UPDATE DeliveryMethods 
                SET MethodName = N'Hẹn lấy tại cửa hàng',
                    Description = N'Khách hàng đến lấy hàng tại cửa hàng'
                WHERE DeliveryMethodID = 2
            ");
        }
    }
}
