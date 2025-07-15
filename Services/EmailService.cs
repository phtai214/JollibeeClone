using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Configuration;
using JollibeeClone.Models;
using Newtonsoft.Json;

namespace JollibeeClone.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendOrderConfirmationEmailAsync(Orders order, List<OrderItems> orderItems)
        {
            try
            {
                _logger.LogInformation($"📧 Starting email send for order {order.OrderCode}");
                Console.WriteLine($"📧 [EmailService] Starting email send for order {order.OrderCode}");
                Console.WriteLine($"📧 [EmailService] Target email: {order.CustomerEmail}");

                // Get email settings from configuration
                var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var senderEmail = _configuration["EmailSettings:SenderEmail"];
                var senderPassword = _configuration["EmailSettings:SenderPassword"];
                var senderName = _configuration["EmailSettings:SenderName"] ?? "Jollibee Vietnam";

                Console.WriteLine($"📧 [EmailService] SMTP Config: {smtpServer}:{smtpPort}, Sender: {senderEmail}");

                if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(senderPassword))
                {
                    var errorMsg = "❌ Email configuration is missing. Please check appsettings.json";
                    _logger.LogError(errorMsg);
                    Console.WriteLine($"📧 [EmailService] {errorMsg}");
                    Console.WriteLine($"📧 [EmailService] SenderEmail empty: {string.IsNullOrEmpty(senderEmail)}");
                    Console.WriteLine($"📧 [EmailService] SenderPassword empty: {string.IsNullOrEmpty(senderPassword)}");
                    return false;
                }

                // Create email content
                var emailBody = GenerateOrderConfirmationEmailBody(order, orderItems);
                var subject = $"Xác nhận đơn hàng #{order.OrderCode} - Jollibee Vietnam";

                // Setup SMTP client
                using var smtpClient = new SmtpClient(smtpServer, smtpPort)
                {
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(senderEmail, senderPassword)
                };

                // Create email message
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = subject,
                    Body = emailBody,
                    IsBodyHtml = true,
                    BodyEncoding = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8
                };

                mailMessage.To.Add(new MailAddress(order.CustomerEmail, order.CustomerFullName));

                // Send email
                await smtpClient.SendMailAsync(mailMessage);
                
                _logger.LogInformation($"✅ Email sent successfully to {order.CustomerEmail} for order {order.OrderCode}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Failed to send email for order {order.OrderCode}: {ex.Message}");
                _logger.LogError($"❌ Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        private string GenerateOrderConfirmationEmailBody(Orders order, List<OrderItems> orderItems)
        {
            var deliveryInfo = "";
            var isPickup = order.DeliveryMethodID != 1; // Assuming 1 is delivery, others are pickup

            if (isPickup && order.PickupDate.HasValue)
            {
                deliveryInfo = $@"
                    <div style=""background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0;"">
                        <h4 style=""color: #856404; margin: 0 0 10px 0;"">🏪 Thông tin nhận hàng</h4>
                        <p style=""margin: 5px 0; color: #856404;""><strong>Ngày nhận:</strong> {order.PickupDate.Value:dd/MM/yyyy}</p>
                        {(order.PickupTimeSlot.HasValue ? $"<p style=\"margin: 5px 0; color: #856404;\"><strong>Thời gian:</strong> {order.PickupTimeSlot.Value:HH\\:mm}</p>" : "")}
                        {(!string.IsNullOrEmpty(order.Store?.StoreName) ? $"<p style=\"margin: 5px 0; color: #856404;\"><strong>Cửa hàng:</strong> {order.Store.StoreName}</p>" : "")}
                        {(!string.IsNullOrEmpty(order.Store?.StreetAddress) ? $"<p style=\"margin: 5px 0; color: #856404;\"><strong>Địa chỉ:</strong> {order.Store.StreetAddress}</p>" : "")}
                    </div>";
            }
            else
            {
                var deliveryAddress = !string.IsNullOrEmpty(order.UserAddress?.Address) ? order.UserAddress.Address : "Địa chỉ giao hàng";
                var estimatedTime = order.OrderDate.AddMinutes(45);
                
                deliveryInfo = $@"
                    <div style=""background-color: #d1ecf1; border-left: 4px solid #17a2b8; padding: 15px; margin: 20px 0;"">
                        <h4 style=""color: #0c5460; margin: 0 0 10px 0;"">🚚 Thông tin giao hàng</h4>
                        <p style=""margin: 5px 0; color: #0c5460;""><strong>Địa chỉ:</strong> {deliveryAddress}</p>
                        <p style=""margin: 5px 0; color: #0c5460;""><strong>Thời gian dự kiến:</strong> {estimatedTime:HH:mm} - {estimatedTime.AddMinutes(15):HH:mm}</p>
                        <p style=""margin: 5px 0; color: #0c5460;""><strong>Phí giao hàng:</strong> {order.ShippingFee:N0}₫</p>
                    </div>";
            }

            // Generate order items HTML
            var orderItemsHtml = new StringBuilder();
            foreach (var item in orderItems)
            {
                var configurationHtml = "";
                
                // Parse configuration if exists
                if (!string.IsNullOrEmpty(item.SelectedConfigurationSnapshot))
                {
                    try
                    {
                        var configData = JsonConvert.DeserializeObject<List<dynamic>>(item.SelectedConfigurationSnapshot);
                        if (configData != null && configData.Any())
                        {
                            var configList = new StringBuilder();
                            var groupedConfigs = configData.GroupBy(c => (string)c.GroupName);
                            
                            foreach (var group in groupedConfigs)
                            {
                                configList.Append($"<div style='margin: 5px 0;'><strong>{group.Key}:</strong> ");
                                var options = group.Select(option => $"{(string)option.OptionProductName}");
                                configList.Append(string.Join(", ", options));
                                configList.Append("</div>");
                            }
                            
                            configurationHtml = $@"
                                <div style=""font-size: 12px; color: #666; margin-top: 5px; padding: 8px; background-color: #f8f9fa; border-radius: 4px;"">
                                    {configList}
                                </div>";
                        }
                    }
                    catch
                    {
                        // Ignore parsing errors
                    }
                }

                orderItemsHtml.Append($@"
                    <tr>
                        <td style=""padding: 15px; border-bottom: 1px solid #ddd;"">
                            <div style=""font-weight: bold; margin-bottom: 5px;"">{item.ProductNameSnapshot}</div>
                            <div style=""font-size: 14px; color: #666;"">Số lượng: {item.Quantity}</div>
                            {configurationHtml}
                        </td>
                        <td style=""padding: 15px; border-bottom: 1px solid #ddd; text-align: right; font-weight: bold;"">{item.UnitPrice:N0}₫</td>
                        <td style=""padding: 15px; border-bottom: 1px solid #ddd; text-align: right; font-weight: bold;"">{item.Subtotal:N0}₫</td>
                    </tr>");
            }

            var emailBody = $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Xác nhận đơn hàng #{order.OrderCode}</title>
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;"">
    
    <!-- Header -->
    <div style=""background: linear-gradient(135deg, #e31e25 0%, #ff4444 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;"">
        <h1 style=""margin: 0; font-size: 28px; font-weight: bold;"">JOLLIBEE VIETNAM</h1>
        <p style=""margin: 10px 0 0 0; font-size: 16px; opacity: 0.9;"">Cảm ơn bạn đã đặt hàng!</p>
    </div>

    <!-- Main Content -->
    <div style=""background-color: #ffffff; padding: 30px; border: 1px solid #ddd; border-top: none;"">
        
        <!-- Greeting -->
        <h2 style=""color: #e31e25; margin-bottom: 20px;"">Xin chào {order.CustomerFullName}!</h2>
        
        <p style=""margin-bottom: 20px; font-size: 16px;"">
            Cảm ơn bạn đã tin tưởng và đặt hàng tại Jollibee Vietnam. Đơn hàng của bạn đã được tiếp nhận và đang được xử lý.
        </p>

        <!-- Order Summary -->
        <div style=""background-color: #f8f9fa; padding: 20px; border-radius: 8px; margin: 25px 0;"">
            <h3 style=""color: #e31e25; margin-top: 0;"">Thông tin đơn hàng</h3>
            <table style=""width: 100%; border-collapse: collapse;"">
                <tr>
                    <td style=""padding: 8px 0; font-weight: bold;"">Mã đơn hàng:</td>
                    <td style=""padding: 8px 0; color: #e31e25; font-weight: bold; font-size: 18px;"">{order.OrderCode}</td>
                </tr>
                <tr>
                    <td style=""padding: 8px 0; font-weight: bold;"">Ngày đặt:</td>
                    <td style=""padding: 8px 0;"">{order.OrderDate:dd/MM/yyyy HH:mm}</td>
                </tr>
                <tr>
                    <td style=""padding: 8px 0; font-weight: bold;"">Số điện thoại:</td>
                    <td style=""padding: 8px 0;"">{order.CustomerPhoneNumber}</td>
                </tr>
                <tr>
                    <td style=""padding: 8px 0; font-weight: bold;"">Phương thức thanh toán:</td>
                    <td style=""padding: 8px 0;"">{order.PaymentMethod?.MethodName ?? "Tiền mặt"}</td>
                </tr>
            </table>
        </div>

        {deliveryInfo}

        <!-- Order Items -->
        <h3 style=""color: #e31e25; margin: 25px 0 15px 0;"">Chi tiết đơn hàng</h3>
        <table style=""width: 100%; border-collapse: collapse; border: 1px solid #ddd; border-radius: 8px; overflow: hidden;"">
            <thead>
                <tr style=""background-color: #e31e25; color: white;"">
                    <th style=""padding: 15px; text-align: left;"">Sản phẩm</th>
                    <th style=""padding: 15px; text-align: right; width: 100px;"">Đơn giá</th>
                    <th style=""padding: 15px; text-align: right; width: 100px;"">Thành tiền</th>
                </tr>
            </thead>
            <tbody>
                {orderItemsHtml}
            </tbody>
        </table>

        <!-- Total Summary -->
        <div style=""margin-top: 20px; padding: 20px; background-color: #f8f9fa; border-radius: 8px;"">
            <table style=""width: 100%; font-size: 16px;"">
                <tr>
                    <td style=""padding: 5px 0;"">Tạm tính:</td>
                    <td style=""text-align: right; padding: 5px 0;"">{order.SubtotalAmount:N0}₫</td>
                </tr>
                <tr>
                    <td style=""padding: 5px 0;"">Phí giao hàng:</td>
                    <td style=""text-align: right; padding: 5px 0;"">{order.ShippingFee:N0}₫</td>
                </tr>
                {(order.DiscountAmount > 0 ? $@"
                <tr>
                    <td style=""padding: 5px 0; color: #28a745;"">Giảm giá:</td>
                    <td style=""text-align: right; padding: 5px 0; color: #28a745;"">-{order.DiscountAmount:N0}₫</td>
                </tr>" : "")}
                <tr style=""border-top: 2px solid #e31e25; font-weight: bold; font-size: 18px; color: #e31e25;"">
                    <td style=""padding: 10px 0;"">TỔNG CỘNG:</td>
                    <td style=""text-align: right; padding: 10px 0;"">{order.TotalAmount:N0}₫</td>
                </tr>
            </table>
        </div>

        {(!string.IsNullOrEmpty(order.NotesByCustomer) ? $@"
        <div style=""margin-top: 20px; padding: 15px; background-color: #fff3cd; border-left: 4px solid #ffc107; border-radius: 4px;"">
            <h4 style=""color: #856404; margin: 0 0 10px 0;"">📝 Ghi chú của bạn</h4>
            <p style=""color: #856404; margin: 0;"">{order.NotesByCustomer}</p>
        </div>" : "")}

        <!-- Contact Information -->
        <div style=""margin-top: 30px; padding: 20px; background-color: #e7f3ff; border-radius: 8px;"">
            <h4 style=""color: #0c5460; margin-top: 0;"">📞 Liên hệ hỗ trợ</h4>
            <p style=""margin: 5px 0; color: #0c5460;""><strong>Hotline:</strong> 1900 1533</p>
            <p style=""margin: 5px 0; color: #0c5460;""><strong>Email:</strong> support@jollibee.com.vn</p>
            <p style=""margin: 5px 0; color: #0c5460;""><strong>Website:</strong> www.jollibee.com.vn</p>
        </div>

        <!-- Footer Message -->
        <div style=""text-align: center; margin-top: 30px; padding: 20px; border-top: 2px solid #e31e25;"">
            <p style=""color: #e31e25; font-weight: bold; font-size: 18px; margin: 0 0 10px 0;"">
                Cảm ơn bạn đã chọn Jollibee Vietnam! ❤️
            </p>
            <p style=""color: #666; margin: 0; font-style: italic;"">
                Chúng tôi luôn nỗ lực mang đến cho bạn những trải nghiệm ẩm thực tuyệt vời nhất.
            </p>
        </div>
    </div>

    <!-- Footer -->
    <div style=""background-color: #333; color: white; text-align: center; padding: 20px; border-radius: 0 0 10px 10px;"">
        <p style=""margin: 0; font-size: 14px;"">
            © 2025 Jollibee Vietnam. Tất cả quyền được bảo lưu.
        </p>
        <p style=""margin: 5px 0 0 0; font-size: 12px; opacity: 0.8;"">
            Email này được gửi tự động, vui lòng không trả lời trực tiếp.
        </p>
    </div>

</body>
</html>";

            return emailBody;
        }
    }
}
