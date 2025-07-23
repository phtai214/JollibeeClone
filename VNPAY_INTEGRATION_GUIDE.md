# Hướng dẫn tích hợp VNPay vào dự án JollibeeClone

## ✅ Những gì đã được thực hiện

1. **VNPayService** - Service xử lý API VNPay
2. **VNPayController** - Controller xử lý callback từ VNPay
3. **VNPayViewModel** - Models cho request/response VNPay
4. **VNPay Processing Page** - Trang chờ thanh toán với UI đẹp
5. **CSS Styling** - Thiết kế chuyên nghiệp cho trang VNPay
6. **CartController Updates** - Logic phân biệt VNPay vs tiền mặt
7. **Configuration** - Cấu hình VNPay trong appsettings.json
8. **Dependency Injection** - Đăng ký services trong Program.cs

## 🔧 Các bước cần thực hiện

### Bước 1: Thêm Logo VNPay
```bash
# Tải logo VNPay và đặt vào:
wwwroot/assets/images/vnpay.png

# Hoặc tìm logo từ: https://vnpay.vn/en/media-kit/
# Kích thước khuyến nghị: 200x80px, PNG với nền trong suốt
```

### Bước 2: Chạy Migration để thêm Order Status
```bash
dotnet ef database update
```

### Bước 3: Kiểm tra cấu hình VNPay
Đảm bảo `appsettings.json` có đúng cấu hình:
```json
{
  "VNPay": {
    "TmnCode": "2QXUI4JG",
    "HashSecret": "ZAGDNEOOYVHBJAXGUKVXEBKFCGUTDHDN", 
    "BaseUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
    "ReturnUrl": "https://localhost:5107/VNPay/Return"
  }
}
```

### Bước 4: Cập nhật URL Return trong VNPay Portal
- Đăng nhập vào VNPay Portal
- Cập nhật Return URL thành: `https://localhost:5107/VNPay/Return`
- Hoặc URL domain thực tế khi deploy production

## 🔄 Flow thanh toán VNPay

### 1. User chọn thanh toán VNPay
- PaymentMethodID = 2 (Ví VNPAY)
- CartController.ProcessCheckout() tạo đơn hàng với status "Chờ thanh toán"

### 2. Redirect đến VNPay
- Tạo VNPayRequestModel với thông tin đơn hàng
- VNPayService.CreatePaymentUrl() tạo URL thanh toán
- User được redirect đến VNPay gateway

### 3. User thanh toán trên VNPay
- User chọn ngân hàng và phương thức thanh toán
- Nhập thông tin thẻ/tài khoản
- VNPay xử lý giao dịch

### 4. VNPay callback
- VNPay redirect về `VNPayController.Return()`
- Validate signature và response từ VNPay
- Cập nhật trạng thái đơn hàng và payment

### 5. Hoàn tất
- Nếu thành công: redirect đến OrderSuccess
- Nếu thất bại: cập nhật status "Hủy", redirect về Home

## 📊 Database Changes

### Order Status mới
```sql
-- Status "Chờ thanh toán" được thêm cho VNPay
INSERT INTO OrderStatuses (StatusName, Description) 
VALUES (N'Chờ thanh toán', N'Đơn hàng đã được tạo và đang chờ thanh toán qua VNPay')
```

### Payment Status Logic
- **VNPay**: Bắt đầu với "Pending", cập nhật thành "Completed" sau callback thành công
- **Tiền mặt**: Ngay lập tức "Completed"

## 🎨 UI/UX Features

### Checkout Page
- Icon VNPay hiển thị trong danh sách payment methods
- Phân biệt rõ ràng với các phương thức khác

### Processing Page  
- Animation loading đẹp mắt
- Hướng dẫn chi tiết cho user
- Security badges tăng tin cậy
- Auto-redirect sau 30 giây nếu có lỗi

### Order Success
- Hiển thị phương thức thanh toán VNPay
- Thông tin transaction ID từ VNPay
- Email confirmation với đúng payment method

## 🔐 Security Features

1. **Signature Validation**: Kiểm tra chữ ký từ VNPay
2. **Amount Verification**: So sánh số tiền với đơn hàng
3. **Transaction Uniqueness**: Mỗi đơn hàng có transaction code riêng
4. **Order Status Protection**: Chỉ cập nhật nếu validation thành công

## 🧪 Testing

### Test Cases cần kiểm tra:
1. **Thanh toán thành công**
   - Chọn VNPay → Redirect đến VNPay → Thanh toán → Về OrderSuccess
   
2. **Thanh toán thất bại**
   - Chọn VNPay → Redirect đến VNPay → Hủy/Thất bại → Về Home với thông báo lỗi
   
3. **User flow so sánh**
   - Tiền mặt: Direct OrderSuccess + Email
   - VNPay: VNPay Gateway → Callback → OrderSuccess + Email

4. **Edge cases**
   - Invalid signature từ VNPay
   - Amount không khớp
   - Order không tồn tại
   - Network timeout

## 🚀 Deployment Notes

### Production Configuration
```json
{
  "VNPay": {
    "TmnCode": "YOUR_PRODUCTION_TMN_CODE",
    "HashSecret": "YOUR_PRODUCTION_HASH_SECRET",
    "BaseUrl": "https://pay.vnpay.vn/vpcpay.html",
    "ReturnUrl": "https://yourdomain.com/VNPay/Return"
  }
}
```

### VNPay Portal Settings
- Cập nhật Return URL thành production domain
- Kiểm tra whitelist IP nếu có
- Test với VNPay sandbox trước khi go live

## 🐛 Troubleshooting

### Lỗi thường gặp:
1. **Invalid Signature**: Kiểm tra HashSecret
2. **Return URL không hoạt động**: Kiểm tra routing và firewall
3. **Amount mismatch**: Đảm bảo x100 cho VNPay API
4. **Order không tìm thấy**: Kiểm tra OrderCode format

### Debug Tools:
- Console.WriteLine trong VNPayController.Return()
- Kiểm tra logs trong VNPay portal
- Test với VNPay sandbox environment

## ✅ Checklist hoàn thiện

- [ ] Thêm logo VNPay vào `wwwroot/assets/images/vnpay.png`
- [ ] Chạy migration: `dotnet ef database update`
- [ ] Test thanh toán VNPay thành công
- [ ] Test thanh toán VNPay thất bại
- [ ] Kiểm tra email confirmation
- [ ] Kiểm tra OrderSuccess page
- [ ] Test responsive design
- [ ] Cấu hình production environment
- [ ] Update VNPay portal settings

## 🎉 Kết quả mong đợi

Sau khi hoàn thành, hệ thống sẽ:
1. Hỗ trợ thanh toán VNPay hoàn chỉnh
2. UI/UX chuyên nghiệp cho VNPay flow
3. Database tracking đầy đủ cho transactions
4. Email notifications với đúng payment method
5. Error handling và security tốt
6. Ready for production deployment

---

**🚨 Lưu ý quan trọng**: Đây là sandbox environment. Nhớ thay đổi cấu hình sang production khi deploy thật! 