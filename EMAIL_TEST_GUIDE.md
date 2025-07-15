# Hướng dẫn Test Email System

## Bước 1: Cấu hình Email

1. Mở file `appsettings.Development.json`
2. Thay đổi cấu hình email:

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": "587",
    "SenderEmail": "your-gmail@gmail.com",
    "SenderPassword": "your-app-password",
    "SenderName": "Jollibee Vietnam"
  }
}
```

**Cần thay thế:**
- `your-gmail@gmail.com` → Địa chỉ Gmail của bạn
- `your-app-password` → App Password từ Google (16 ký tự)

## Bước 2: Tạo App Password Gmail

1. Vào [Google Account Settings](https://myaccount.google.com/)
2. Chọn **Security** → **2-Step Verification** (bật nếu chưa có)
3. Tìm **App passwords** → **Select app** → **Other**
4. Nhập tên: "Jollibee Website"
5. Copy mật khẩu 16 ký tự được tạo

## Bước 3: Test Email Đơn Giản

### Sử dụng Test Endpoint

1. Chạy website (`dotnet run` hoặc F5)
2. Mở browser và truy cập:
   ```
   https://localhost:7xxx/Cart/TestEmail?email=your-test-email@gmail.com
   ```
3. Thay `your-test-email@gmail.com` bằng email thật để nhận test

### Kết quả mong đợi:

✅ **Thành công:**
```json
{
  "success": true,
  "message": "✅ Test email sent successfully to your-test-email@gmail.com",
  "orderCode": "TEST20250715123456",
  "recipient": "your-test-email@gmail.com"
}
```

❌ **Thất bại:**
```json
{
  "success": false,
  "message": "❌ Failed to send test email to your-test-email@gmail.com. Check console logs for details."
}
```

## Bước 4: Test Email Thực Tế

1. Đặt một đơn hàng bình thường qua website
2. Sử dụng email thật khi checkout
3. Sau khi đặt hàng thành công, kiểm tra email
4. Xem Console log để debug nếu cần

## Bước 5: Kiểm tra Console Logs

Trong quá trình test, quan sát Console để thấy:

```
🛒 ProcessCheckout - Creating order for customer: Nguyen Van A
✅ Order JB240715001 created successfully!
📧 Starting email send for order JB240715001
✅ Email sent successfully to customer@email.com for order JB240715001
```

## Email Template Preview

Email được gửi sẽ có giao diện chuyên nghiệp với:

- **Header đỏ Jollibee** với lời chào
- **Thông tin đơn hàng**: Mã đơn, ngày đặt, SĐT, thanh toán
- **Chi tiết giao hàng/nhận hàng**: Địa chỉ, thời gian
- **Danh sách sản phẩm**: Tên, cấu hình, số lượng, giá
- **Tổng kết tiền**: Tạm tính, ship, giảm giá, tổng cộng
- **Thông tin liên hệ**: Hotline, email hỗ trợ
- **Footer cảm ơn**

## Troubleshooting

### Lỗi thường gặp:

1. **"Authentication failed"**
   - Kiểm tra email và App Password
   - Đảm bảo 2-Step Verification đã bật

2. **"SMTP connection failed"**
   - Kiểm tra internet connection
   - Firewall có block port 587 không

3. **Email vào Spam**
   - Thêm sender email vào whitelist
   - Đây là bình thường với Gmail SMTP

4. **"Missing email configuration"**
   - Kiểm tra appsettings.json có đúng format không
   - Restart application sau khi sửa config

### Debug Console Messages:

- `📧 Starting email send for order XXX` → Bắt đầu gửi email
- `✅ Email sent successfully` → Gửi thành công
- `❌ Failed to send email` → Gửi thất bại
- `⚠️ Skipping email` → Bỏ qua (thiếu email hoặc dữ liệu)

## Next Steps

Sau khi test thành công:

1. **Production**: Chuyển sang SendGrid hoặc AWS SES
2. **Customization**: Tùy chỉnh template trong `EmailService.cs`
3. **Monitoring**: Thêm logging và metrics
4. **Features**: Email order status updates, promotional emails

---

**Lưu ý**: Test endpoint `/Cart/TestEmail` chỉ dùng cho development. Xóa hoặc bảo mật endpoint này trước khi deploy production.
