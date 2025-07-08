using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JollibeeClone.Migrations
{
    /// <inheritdoc />
    public partial class FixNewsTypeConstraintToEnglish : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_NewsType_News",
                table: "News");

            migrationBuilder.AddCheckConstraint(
                name: "CK_NewsType_News",
                table: "News",
                sql: "NewsType IN ('News', 'Promotion')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_NewsType_News",
                table: "News");

            migrationBuilder.AddCheckConstraint(
                name: "CK_NewsType_News",
                table: "News",
                sql: "NewsType IN ('Tin tức', 'Khuyến mãi')");
        }
    }
}
