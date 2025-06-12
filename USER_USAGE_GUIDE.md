# Hướng dẫn sử dụng tính năng User Usage trong Voucher System

## Tổng quan
Tính năng này cho phép admin xem **ai đã sử dụng voucher nào**, **bao nhiêu lần**, và **thống kê chi tiết** về việc sử dụng voucher của khách hàng.

## Tính năng mới đã thêm

### 1. API Endpoint mới trong PromotionController

#### `GetPromotionUsageStatistics/{promotionId}`
- **URL**: `/Admin/Promotion/UsageStatistics/{promotionId}`
- **Method**: GET
- **Mô tả**: Lấy thống kê ai đã sử dụng voucher này
- **Response**:
```json
{
  "success": true,
  "usageStats": [
    {
      "userPromotionId": 1,
      "userId": 1,
      "userFullName": "Nguyễn Văn A",
      "userEmail": "user@example.com",
      "discountAmount": 50000,
      "usedDate": "2025-01-07T10:30:00",
      "orderId": 1,
      "orderCode": "ORD001",
      "orderTotal": 200000
    }
  ],
  "summary": {
    "totalUsers": 5,
    "totalDiscountGiven": 300000,
    "averageDiscountPerUser": 60000
  }
}
```

### 2. Giao diện mới trong Details Page

#### Thêm section "Lịch sử sử dụng voucher"
- **Vị trí**: Cuối trang Details của voucher
- **Hiển thị**:
  - 4 cards thống kê tổng quan
  - Bảng chi tiết users đã sử dụng
  - Thông tin đơn hàng liên quan
  - Nút refresh dữ liệu

#### Các thành phần giao diện:

**1. Summary Cards:**
- Tổng khách hàng
- Tổng tiền giảm giá  
- Trung bình giảm giá/người
- Số lượt sử dụng hôm nay

**2. Usage Table:**
- Tên khách hàng + avatar
- Email (có link mailto)
- Số tiền giảm giá
- Ngày giờ sử dụng
- Thông tin đơn hàng (nếu có)
- Nút thao tác (xem chi tiết user, xem đơn hàng)

## Cách sử dụng

### 1. Xem thống kê sử dụng voucher

1. **Vào trang quản lý voucher**: `/Admin/Promotion`
2. **Click "Chi tiết"** của voucher muốn xem
3. **Scroll xuống** phần "Lịch sử sử dụng voucher"
4. **Xem thông tin**:
   - Cards thống kê ở trên
   - Bảng chi tiết ở dưới

### 2. Các thao tác có thể thực hiện

#### Trong giao diện:
- **Làm mới dữ liệu**: Click nút "Làm mới"
- **Gửi email cho user**: Click vào email trong bảng
- **Xem đơn hàng**: Click vào mã đơn hàng (nếu có)
- **Xem chi tiết user**: Click nút mắt (icon eye)

#### Highlights đặc biệt:
- **Rows có màu vàng**: Những lượt sử dụng hôm nay
- **Badge "Hôm nay"**: Hiển thị bên cạnh tên user đã dùng hôm nay
- **Auto-refresh**: Dữ liệu tự động refresh mỗi 30 giây
- **Format tiền tệ**: Hiển thị theo định dạng VND

### 3. Test dữ liệu

#### Để test tính năng, chạy script SQL:
```sql
-- Chạy script này để thêm test data
sqlcmd -S localhost -d Jollibee_DB -E -i simple_test_data.sql
```

#### Hoặc thêm manual:
```sql
INSERT INTO UserPromotions (UserID, PromotionID, UsedDate, DiscountAmount, OrderID)
VALUES 
    (1, 1, GETDATE(), 50000, NULL),
    (1, 1, DATEADD(hour, -2, GETDATE()), 75000, NULL),
    (1, 1, DATEADD(day, -1, GETDATE()), 30000, NULL);
```

## Lợi ích của tính năng

### 1. Cho Admin
- **Theo dõi hiệu quả voucher**: Xem ai đã sử dụng, bao nhiều lần
- **Phân tích khách hàng**: Biết khách hàng nào thích dùng voucher
- **Kiểm soát gian lận**: Phát hiện nếu có user abuse system
- **Báo cáo doanh số**: Tính tổng tiền đã giảm giá

### 2. Cho Business
- **Tối ưu chiến lược marketing**: Biết voucher nào hiệu quả
- **Segment khách hàng**: Phân loại khách theo hành vi dùng voucher  
- **Forecast**: Dự đoán lượng voucher sẽ được sử dụng
- **ROI tracking**: Đo lường hiệu quả đầu tư vào voucher

## Cấu trúc kỹ thuật

### Database
- **Bảng chính**: `UserPromotions`
- **Relationships**: User ↔ UserPromotions ↔ Promotion ↔ Orders
- **Indexes**: Có index trên UserID, PromotionID để query nhanh

### API Architecture
- **Controller**: `PromotionController`
- **Service**: `IPromotionService` (đã có từ trước)
- **Response Format**: JSON với structure chuẩn
- **Error Handling**: Try-catch với log đầy đủ

### Frontend
- **Technology**: jQuery + Bootstrap 5
- **AJAX**: Fetch API cho modern browsers
- **Auto-refresh**: setInterval every 30 seconds
- **Responsive**: Mobile-friendly table với scroll

## Troubleshooting

### 1. Không có dữ liệu hiển thị
- **Kiểm tra**: Có user nào đã sử dụng voucher chưa?
- **Solution**: Chạy script test data hoặc test thực tế

### 2. Lỗi API call
- **Check console**: F12 → Console tab xem lỗi
- **Check server**: Xem log trong Visual Studio
- **Check database**: Confirm UserPromotions table exists

### 3. Hiển thị sai format
- **Check timezone**: Confirm server và client cùng timezone
- **Check culture**: Confirm vi-VN culture settings
- **Check CSS**: Confirm Bootstrap và custom CSS load đúng

## Tương lai phát triển

### Phase 2 - Advanced features:
1. **Pagination**: Phân trang khi có nhiều data
2. **Export**: Xuất Excel báo cáo sử dụng voucher
3. **Filters**: Lọc theo ngày, user, amount
4. **Charts**: Biểu đồ thống kê xu hướng sử dụng
5. **Real-time**: WebSocket để update real-time
6. **User Profile**: Click vào user để xem profile chi tiết

### Phase 3 - Analytics:
1. **Heat map**: Xem voucher được dùng vào thời gian nào
2. **Cohort analysis**: Phân tích nhóm khách hàng
3. **Predictive**: AI dự đoán voucher sẽ được sử dụng
4. **A/B Testing**: Test hiệu quả các loại voucher khác nhau

---

**Note**: Tính năng này hoạt động độc lập với hệ thống cũ và không ảnh hưởng đến các chức năng đã có. 