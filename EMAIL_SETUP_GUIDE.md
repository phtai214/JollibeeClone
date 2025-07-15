# Hướng dẫn cấu hình Email cho Jollibee Clone

## 1. Tổng quan

Hệ thống email được triển khai để gửi email xác nhận đơn hàng tự động sau khi khách hàng đặt hàng thành công. Email có giao diện chuyên nghiệp với đầy đủ thông tin đơn hàng.

## 2. Cách cấu hình Gmail SMTP

### Bước 1: Tạo App Password cho Gmail

1. Đăng nhập vào tài khoản Gmail của bạn
2. Vào **Google Account Settings** → **Security**
3. Bật **2-Step Verification** (nếu chưa bật)
4. Tìm **App passwords** và chọn **Select app** → **Other (custom name)**
5. Nhập tên: "Jollibee Clone Website"
6. Nhấn **Generate** và copy mật khẩu 16 ký tự

### Bước 2: Cấu hình trong appsettings.json

Mở file `appsettings.Development.json` và cập nhật:

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": "587",
    "SenderEmail": "your-email@gmail.com",
    "SenderPassword": "your-16-character-app-password",
    "SenderName": "Jollibee Vietnam"
  }
}
```

**Thay thế:**
- `your-email@gmail.com` → Email Gmail của bạn
- `your-16-character-app-password` → App password vừa tạo

### Bước 3: Test Email

1. Chạy website
2. Đặt một đơn hàng test với email thật
3. Kiểm tra Console log xem có thông báo email gửi thành công không

## 3. Cấu trúc Email Template

Email được thiết kế chuyên nghiệp với:

- **Header**: Logo Jollibee với gradient đỏ
- **Thông tin đơn hàng**: Mã đơn, ngày đặt, thông tin khách hàng
- **Chi tiết giao hàng/nhận hàng**: Địa chỉ, thời gian dự kiến
- **Danh sách sản phẩm**: Tên, cấu hình, số lượng, giá
- **Tổng kết**: Tạm tính, phí ship, giảm giá, tổng cộng
- **Liên hệ hỗ trợ**: Hotline, email, website
- **Footer**: Thông điệp cảm ơn

## 4. Tính năng đặc biệt

### Gửi email không đồng bộ
- Email được gửi trong background task
- Không làm chậm quá trình checkout
- User được redirect ngay đến trang success

### Xử lý cấu hình sản phẩm
- Parse và hiển thị đầy đủ configuration options
- Hiển thị variant, toppings, size...
- Format đẹp và dễ đọc

### Responsive Design
- Email hiển thị tốt trên mọi device
- Mobile-friendly layout
- Font và color scheme nhất quán với brand

## 5. Xử lý lỗi

Hệ thống có logging chi tiết:
- ✅ Success: "Email sent successfully"  
- ❌ Failed: "Failed to send email" với chi tiết lỗi
- ⚠️ Skip: Khi thiếu email hoặc dữ liệu

## 6. Bảo mật

- App password được sử dụng thay vì mật khẩu chính
- Credentials stored trong appsettings (không commit lên git)
- SSL/TLS encryption cho SMTP connection

## 7. Customization

Có thể dễ dàng tùy chỉnh:
- **EmailService.cs**: Logic gửi email
- **GenerateOrderConfirmationEmailBody()**: Template HTML
- **appsettings.json**: Cấu hình SMTP

## 8. Troubleshooting

### Email không gửi được:
1. Kiểm tra App Password đúng chưa
2. Kiểm tra 2-Step Verification đã bật chưa  
3. Kiểm tra network có block port 587 không
4. Xem Console log để debug

### Email vào spam:
1. Thêm sender email vào whitelist
2. Có thể cần SPF/DKIM records cho domain production

## 9. Production Deployment

Cho production environment:
1. Sử dụng dedicated email service (SendGrid, AWS SES)
2. Setup SPF, DKIM, DMARC records
3. Monitor email delivery rates
4. Implement email queue system

## 10. Example Console Output

```
🛒 ProcessCheckout - Creating order for customer: Nguyen Van A
✅ Order JB240715001 created successfully!
📧 Starting email send for order JB240715001
✅ Email sent successfully to customer@email.com for order JB240715001
```

---

**Lưu ý**: Đây là cài đặt cơ bản cho development. Với production, nên sử dụng email service chuyên nghiệp như SendGrid hay AWS SES để đảm bảo deliverability cao hơn.
