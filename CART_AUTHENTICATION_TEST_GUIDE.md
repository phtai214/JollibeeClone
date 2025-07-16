# Hướng Dẫn Test Tính Năng Authentication cho Cart

## Tổng Quan Tính Năng

Đã triển khai ràng buộc authentication cho chức năng thanh toán trong giỏ hàng:

- ✅ **User đã login**: Redirect trực tiếp đến trang shipping/checkout  
- ✅ **User chưa login**: Hiển thị modal yêu cầu đăng nhập với button link đến trang login
- ✅ **Sau khi login**: Redirect về trang shipping để tiếp tục checkout
- ✅ **Giữ nguyên cart session**: Cart được preserve trong suốt quá trình

## Các File Đã Thay Đổi

### 1. **wwwroot/js/cart.js**
- Thêm method `checkUserAuthentication()` - gọi API kiểm tra authentication
- Cập nhật `proceedToCheckout()` - thêm logic check authentication 
- Thêm method `showLoginModal()` - hiển thị modal đẹp mắt yêu cầu login

### 2. **Controllers/AccountController.cs**
- Thêm API endpoint `CheckAuthenticationStatus()` - trả về trạng thái authentication
- Cập nhật `Login()` methods - hỗ trợ returnUrl parameter 
- Thêm logic redirect về shipping sau khi login từ cart

### 3. **Views/Account/Login.cshtml**
- Thêm hidden field để truyền returnUrl qua form

## Các Bước Test

### Test Case 1: User Chưa Login
1. **Mở trang web** và **chưa đăng nhập**
2. **Thêm sản phẩm** vào giỏ hàng từ menu
3. **Mở cart** (click floating cart button)
4. **Click "Thanh Toán"**
5. **Expected**: Modal login xuất hiện với:
   - Tiêu đề "Yêu cầu đăng nhập"
   - Icon lock đẹp mắt
   - Message rõ ràng
   - Button "Đăng nhập ngay" (redirect đến `/Account/Login?returnUrl=%2FCart%2FShipping`)
   - Button "Tiếp tục mua sắm"
   - Link "Đăng ký tại đây"

### Test Case 2: Login từ Cart Modal
1. **Thực hiện Test Case 1** đến bước hiển thị modal
2. **Click "Đăng nhập ngay"**
3. **Nhập thông tin** đăng nhập hợp lệ
4. **Submit form**
5. **Expected**: 
   - Login thành công
   - Redirect trực tiếp đến `/Cart/Shipping` (không về Home)
   - Cart items vẫn còn nguyên

### Test Case 3: User Đã Login  
1. **Đăng nhập** trước
2. **Thêm sản phẩm** vào cart
3. **Click "Thanh Toán"** trong cart
4. **Expected**: 
   - Không hiển thị modal
   - Redirect trực tiếp đến `/Cart/Shipping`

### Test Case 4: Session Cart Preservation
1. **Chưa login**, thêm **3-4 sản phẩm** vào cart
2. **Kiểm tra** cart có đủ items
3. **Click thanh toán** → modal xuất hiện
4. **Đăng nhập** qua modal
5. **Expected**: 
   - Sau login, cart vẫn có đủ 3-4 items
   - Có thể proceed checkout bình thường

## API Endpoints Mới

### `GET /Account/CheckAuthenticationStatus`
```json
// Response khi đã login
{
  "isAuthenticated": true,
  "userId": "123",
  "userName": "Nguyễn Văn A"
}

// Response khi chưa login  
{
  "isAuthenticated": false,
  "userId": null,
  "userName": null
}
```

## Ghi Chú Kỹ Thuật

- **Cart Session**: Sử dụng session ID để lưu cart, không bị mất khi login
- **Return URL**: Sử dụng URL encoding `%2FCart%2FShipping` cho `/Cart/Shipping`
- **Modal Bootstrap**: Sử dụng Bootstrap 5 modal với auto cleanup
- **Security**: Chỉ cho phép local URL trong returnUrl (bảo mật)

## Troubleshooting

### Modal không hiển thị
- Kiểm tra Bootstrap JS đã load
- Check console errors trong browser

### Authentication API fails
- Kiểm tra session configuration trong ASP.NET Core
- Verify AccountController route

### Return URL không hoạt động
- Ensure returnUrl được encode đúng
- Check `Url.IsLocalUrl()` logic

---

**Status**: ✅ **READY FOR TESTING**

Tất cả tính năng đã được implement và sẵn sàng cho test end-to-end. 