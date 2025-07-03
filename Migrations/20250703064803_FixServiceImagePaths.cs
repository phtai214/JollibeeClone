using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JollibeeClone.Migrations
{
    /// <inheritdoc />
    public partial class FixServiceImagePaths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Sửa đường dẫn hình ảnh cho các dịch vụ
            migrationBuilder.Sql(@"
                UPDATE Services 
                SET ImageUrl = '/assets/images/BannerJollibeeSinhNhat_dichvu.png'
                WHERE ServiceName = N'Đặt tiệc sinh nhật';
                
                UPDATE Services 
                SET ImageUrl = '/assets/images/BannerJollibeeKidClub_dichvu.png'
                WHERE ServiceName = N'Jollibee Kid Club';
                
                UPDATE Services 
                SET ImageUrl = '/assets/images/bannerhotline_dichvu.png'
                WHERE ServiceName = N'Đặt hàng qua hotline';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Khôi phục đường dẫn hình ảnh cũ
            migrationBuilder.Sql(@"
                UPDATE Services 
                SET ImageUrl = '/wwwroot/assets/images/BannerJollibeeSinhNhat_dichvu.png'
                WHERE ServiceName = N'Đặt tiệc sinh nhật';
                
                UPDATE Services 
                SET ImageUrl = '/wwwroot/assets/images/BannerJollibeeKidClub_dichvu.png'
                WHERE ServiceName = N'Jollibee Kid Club';
                
                UPDATE Services 
                SET ImageUrl = '/wwwroot/assets/images/bannerhotline_dichvu.png'
                WHERE ServiceName = N'Đặt hàng qua hotline';
            ");
        }
    }
}
