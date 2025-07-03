# Hướng dẫn sử dụng hệ thống Ưu đãi & Khuyến mãi - Phía Người dùng

## Tổng quan
Hệ thống ưu đãi & khuyến mãi của Jollibee giúp khách hàng dễ dàng tìm kiếm, xem và sử dụng các ưu đãi đang có sẵn.

## Truy cập trang Ưu đãi
- **URL:** `/Promotion` hoặc `/Promotion/Index`
- **Từ menu:** Dịch vụ → Khuyến mãi
- **Link trực tiếp:** Nhấp vào các banner khuyến mãi trên trang chủ

## Giao diện trang Ưu đãi

### 1. Hero Section (Phần đầu trang)
- Hiển thị tiêu đề chính "ƯU ĐÃI & KHUYẾN MÃI"
- Thống kê số lượng ưu đãi đang có
- Hình ảnh mascot Jollibee với hiệu ứng floating
- Background gradient đẹp mắt với các circle decoration

### 2. Search Section (Phần tìm kiếm)
- **Tìm kiếm real-time:** Gõ từ khóa để tìm ưu đãi
- **Hỗ trợ tìm kiếm:** Tên ưu đãi, mã giảm giá, mô tả
- **Responsive:** Thân thiện với mobile

### 3. Promotions Grid (Danh sách ưu đãi)
Mỗi card ưu đãi bao gồm:

#### Promotion Badge (Thẻ giảm giá)
- Hiển thị % hoặc số tiền giảm giá
- Đặt ở góc phải trên của card
- Màu vàng Jollibee nổi bật

#### Promotion Header (Đầu card)
- **Icon:** Biểu tượng quà tặng
- **Countdown Timer:** Thời gian còn lại
  - Hiển thị theo format: "X ngày Y giờ" hoặc "X giờ Y phút"
  - Chuyển màu đỏ khi còn dưới 24 giờ
  - Cập nhật real-time mỗi giây

#### Promotion Content (Nội dung)
- **Tiêu đề:** Tên ưu đãi
- **Mô tả:** Chi tiết về ưu đãi
- **Mã giảm giá:** (nếu có)
  - Click để copy vào clipboard
  - Hiệu ứng visual feedback khi copy thành công
- **Điều kiện:** Giá trị đơn hàng tối thiểu
- **Số lượt sử dụng:** Còn lại hoặc không giới hạn

#### Promotion Actions (Hành động)
- **Nút "Sử dụng ngay":** 
  - Áp dụng ưu đãi (hiện tại chỉ hiển thị thông báo)
  - Có thể mở rộng để chuyển đến giỏ hàng
- **Nút "Chia sẻ":**
  - Sử dụng Web Share API nếu có
  - Fallback: copy link chia sẻ vào clipboard

#### Promotion Footer (Chân card)
- Hiển thị thời gian có hiệu lực (từ ngày - đến ngày)

## Các tính năng đặc biệt

### 1. Real-time Search
- Tìm kiếm ngay khi gõ (debounce 300ms)
- Hiển thị kết quả phù hợp ngay lập tức
- Empty state khi không tìm thấy kết quả

### 2. Countdown Timer
- Cập nhật mỗi giây
- Hiển thị thời gian còn lại chính xác
- Visual cues cho ưu đãi sắp hết hạn
- Tự động đánh dấu ưu đãi đã hết hạn

### 3. Copy to Clipboard
- Modern Clipboard API
- Fallback cho trình duyệt cũ
- Toast notifications cho feedback
- Visual feedback trên element được copy

### 4. Share Functionality
- Web Share API cho mobile
- Fallback copy link cho desktop
- Formatted share text với emoji
- Toast notifications

### 5. Responsive Design
- Mobile-first approach
- Grid layout tự động điều chỉnh
- Touch-friendly buttons trên mobile
- Optimized cho tất cả screen size

### 6. Animations & UX
- Smooth scroll animations
- Hover effects trên cards
- Loading states cho buttons
- Staggered animation cho cards
- Intersection Observer cho performance

## Hướng dẫn sử dụng cho khách hàng

### Tìm kiếm ưu đãi
1. Vào trang Ưu đãi
2. Gõ từ khóa vào ô tìm kiếm
3. Kết quả hiển thị ngay lập tức
4. Click "Xem tất cả ưu đãi" để reset search

### Sao chép mã giảm giá
1. Tìm ưu đãi có mã giảm giá
2. Click vào mã giảm giá (có icon copy)
3. Mã sẽ được copy vào clipboard
4. Paste khi thanh toán

### Sử dụng ưu đãi
1. Click nút "Sử dụng ngay"
2. System sẽ ghi nhận việc sử dụng
3. Chuyển đến trang menu/giỏ hàng (tương lai)

### Chia sẻ ưu đãi
1. Click nút "Chia sẻ"
2. Chọn app để chia sẻ (mobile) hoặc
3. Link được copy để chia sẻ thủ công

## Trạng thái ưu đãi

### Active (Đang hoạt động)
- Hiển thị bình thường
- Countdown timer chạy
- Có thể sử dụng được

### Expiring Soon (Sắp hết hạn)
- Countdown timer chuyển màu đỏ
- Hiển thị "urgent" styling
- Còn dưới 24 giờ

### Expired (Đã hết hạn)
- Opacity giảm xuống 60%
- Timer hiển thị "Đã hết hạn"
- Không thể sử dụng

## Technical Notes

### Performance Optimizations
- Intersection Observer cho animations
- Debounced search
- Efficient countdown updates
- CSS-only animations where possible

### Browser Compatibility
- Modern browsers: Full features
- Older browsers: Graceful degradation
- Mobile: Optimized experience
- Accessibility: ARIA labels, keyboard navigation

### Error Handling
- Network errors: Toast notifications
- Invalid actions: User-friendly messages
- Fallbacks for unsupported features

## API Endpoints (cho developers)

### GET /Promotion
- Hiển thị trang danh sách ưu đãi
- Filter: chỉ active promotions trong thời gian hiệu lực

### GET /Promotion/GetPromotionDetails/{id}
- Lấy chi tiết promotion theo ID
- Response: JSON với thông tin đầy đủ

### POST /Promotion/ValidateCouponCode
- Validate mã giảm giá
- Request: { couponCode, orderAmount, userId }
- Response: { success, data, message }

## Customization

### CSS Variables
```css
:root {
    --jollibee-red: #e31937;
    --jollibee-yellow: #ffc627;
    --jollibee-orange: #ff6b35;
}
```

### JavaScript Events
- `initializePromotions()`: Initialize toàn bộ system
- `copyCouponCode(code)`: Copy mã giảm giá
- `sharePromotion(name, code)`: Chia sẻ ưu đãi

## Troubleshooting

### Không hiển thị ưu đãi
- Kiểm tra có ưu đãi active trong database
- Check thời gian hiệu lực
- Verify database connection

### Countdown không chạy
- Check JavaScript console for errors
- Verify date format từ server
- Ensure timezone consistency

### Copy không hoạt động
- Modern browsers: Check HTTPS/localhost
- Older browsers: Fallback tự động
- Mobile: Check permissions

### Search không hoạt động
- Check JavaScript console
- Verify data attributes trên cards
- Ensure search input có đúng ID

## Updates & Maintenance

### Regular Tasks
- Clean up expired promotions
- Update promotion content
- Monitor performance metrics
- User feedback analysis

### Future Enhancements
- Integration với shopping cart
- User-specific promotions
- Push notifications
- Analytics tracking
- A/B testing framework

---

*Hướng dẫn này được cập nhật lần cuối: Tháng 1/2025* 