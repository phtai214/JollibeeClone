using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JollibeeClone.Migrations
{
    /// <inheritdoc />
    public partial class AddSampleServicesData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Thêm dữ liệu mẫu cho Services
            migrationBuilder.InsertData(
                table: "Services",
                columns: new[] { "ServiceName", "ShortDescription", "Content", "ImageUrl", "DisplayOrder", "IsActive" },
                values: new object[,]
                {
                    {
                        "Đặt tiệc sinh nhật",
                        "Bạn đang tìm tưởng cho một buổi tiệc sinh nhật thật đặc biệt dành cho con của bạn? Hãy chọn những bữa tiệc của Jollibee. Sẽ có nhiều điều vui nhồn và rất đáng nhớ dành cho con của bạn.",
                        "<p>Jollibee cung cấp dịch vụ tổ chức tiệc sinh nhật hoàn hảo cho bé yêu của bạn:</p><ul><li>Gói tiệc đa dạng phù hợp với mọi ngân sách</li><li>Trang trí bóng bay và backdrop theo chủ đề Jollibee</li><li>Mascot Jollibee và bạn bè xuất hiện tại tiệc</li><li>Hoạt động vui chơi hấp dẫn cho trẻ em</li><li>Thực đơn phong phú với các món ăn yêu thích</li><li>Nhân viên tận tình hỗ trợ trong suốt buổi tiệc</li></ul>",
                        "/wwwroot/assets/images/BannerJollibeeSinhNhat_dichvu.png",
                        1,
                        true
                    },
                    {
                        "Jollibee Kid Club",
                        "Hãy để con bạn thoả thích thể hiện và khám phá tài năng bên trong của mình cùng cơ hội gặp gỡ những bạn đồng lứa khác tại Jollibee Kids Club. Cùng tìm hiểu thêm thông tin về Jollibee Kids Club và tham gia ngay.",
                        "<p>Jollibee Kids Club - Câu lạc bộ dành riêng cho các bé:</p><ul><li>Hoạt động giáo dục và giải trí phù hợp với lứa tuổi</li><li>Các buổi workshop sáng tạo và kỹ năng sống</li><li>Gặp gỡ và kết bạn với nhiều bạn cùng trang lứa</li><li>Ưu đãi đặc biệt cho thành viên Kids Club</li><li>Quà tặng và phần thưởng hấp dẫn</li><li>Chương trình sinh nhật đặc biệt cho thành viên</li></ul>",
                        "/wwwroot/assets/images/BannerJollibeeKidClub_dichvu.png",
                        2,
                        true
                    },
                    {
                        "Đặt hàng qua hotline",
                        "Gọi ngay hotline để đặt hàng nhanh chóng và tiện lợi. Chúng tôi sẵn sàng phục vụ bạn 24/7 với thái độ thân thiện và chuyên nghiệp.",
                        "<p>Dịch vụ đặt hàng qua hotline:</p><ul><li>Hotline: 1900 1533 hoạt động 24/7</li><li>Nhân viên tư vấn chuyên nghiệp</li><li>Thời gian giao hàng nhanh chóng</li><li>Thanh toán linh hoạt (tiền mặt, thẻ, chuyển khoản)</li><li>Miễn phí giao hàng trong bán kính 3km</li><li>Theo dõi đơn hàng realtime</li></ul>",
                        "/wwwroot/assets/images/bannerhotline_dichvu.png",
                        3,
                        true
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Xóa dữ liệu mẫu khi rollback
            migrationBuilder.DeleteData(
                table: "Services",
                keyColumn: "ServiceName",
                keyValues: new object[]
                {
                    "Đặt tiệc sinh nhật",
                    "Jollibee Kid Club",
                    "Đặt hàng qua hotline"
                });
        }
    }
}
