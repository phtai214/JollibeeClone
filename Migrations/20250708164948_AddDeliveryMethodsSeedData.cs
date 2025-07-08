using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JollibeeClone.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryMethodsSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Insert delivery methods data only if not exists
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM DeliveryMethods WHERE DeliveryMethodID = 1)
                BEGIN
                    SET IDENTITY_INSERT DeliveryMethods ON;
                    INSERT INTO DeliveryMethods (DeliveryMethodID, MethodName, Description, IsActive)
                    VALUES (1, N'Giao hàng tận nơi', N'Giao hàng tận nơi theo địa chỉ khách hàng', 1);
                    SET IDENTITY_INSERT DeliveryMethods OFF;
                END

                IF NOT EXISTS (SELECT 1 FROM DeliveryMethods WHERE DeliveryMethodID = 2)
                BEGIN
                    SET IDENTITY_INSERT DeliveryMethods ON;
                    INSERT INTO DeliveryMethods (DeliveryMethodID, MethodName, Description, IsActive)
                    VALUES (2, N'Hẹn lấy tại cửa hàng', N'Khách hàng đến lấy hàng tại cửa hàng', 1);
                    SET IDENTITY_INSERT DeliveryMethods OFF;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove delivery methods data
            migrationBuilder.DeleteData(
                table: "DeliveryMethods",
                keyColumn: "DeliveryMethodID",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "DeliveryMethods",
                keyColumn: "DeliveryMethodID",
                keyValue: 2);
        }
    }
}
