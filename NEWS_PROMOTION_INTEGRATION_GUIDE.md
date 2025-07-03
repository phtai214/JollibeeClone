# Hướng dẫn tích hợp Tin tức và Khuyến mãi từ Admin

## Tổng quan

Hệ thống đã được cập nhật để sử dụng dữ liệu tin tức và khuyến mãi từ trang quản trị admin thay vì dữ liệu hardcode hoặc từ bảng Promotion riêng biệt.

## Các thay đổi chính

### 1. Cập nhật NewsController

**File**: `Controllers/NewsController.cs`

- **Trước**: Controller rỗng, chỉ return View()
- **Sau**: 
  - Lấy dữ liệu từ bảng `News` với `NewsType = "News"`
  - Bao gồm thông tin author
  - Hỗ trợ API để lấy chi tiết tin tức
  - Chỉ hiển thị tin tức đã xuất bản (`IsPublished = true`)

### 2. Tạo Views/News/Index.cshtml

**File**: `Views/News/Index.cshtml`

- Thiết kế responsive với grid layout
- Hero section với thống kê số lượng tin tức
- Chức năng tìm kiếm tin tức
- Modal hiển thị chi tiết tin tức
- Nút chia sẻ tin tức

### 3. Cập nhật PromotionController

**File**: `Controllers/PromotionController.cs`

- **Trước**: Lấy dữ liệu từ bảng `Promotions`
- **Sau**: 
  - Lấy dữ liệu từ bảng `News` với `NewsType = "Promotion"`
  - Giữ lại API `ValidateCouponCode` cho tương thích với hệ thống cart
  - Thêm API `GetPromotionDetails` để hiển thị chi tiết

### 4. Cập nhật Views/Promotion/Index.cshtml

**Thay đổi cấu trúc card:**
- **Trước**: Hiển thị discount percentage, coupon code, timer
- **Sau**: 
  - Hiển thị hình ảnh khuyến mãi
  - Thông tin tác giả và ngày xuất bản
  - Nút "Xem chi tiết" thay vì "Sử dụng ngay"
  - Modal hiển thị nội dung khuyến mãi đầy đủ

### 5. Cập nhật HomeController và Views/Home/Index.cshtml

**File**: `Controllers/HomeController.cs`
- Thêm dependency injection cho `AppDbContext`
- Lấy 4 tin tức mới nhất để hiển thị trên trang chủ

**File**: `Views/Home/Index.cshtml`
- **Trước**: Hardcode 4 tin tức cố định
- **Sau**: 
  - Sử dụng dữ liệu động từ `ViewBag.LatestNews`
  - Hiển thị hình ảnh, tiêu đề, mô tả từ database
  - Link đến trang tin tức chi tiết

### 6. Cập nhật CSS

**File**: `wwwroot/css/promotions.css`

Thêm các style mới:
- `.promotion-image`: Style cho hình ảnh khuyến mãi
- `.promotion-placeholder`: Placeholder khi không có hình
- `.promotion-overlay`: Overlay hiển thị ngày tháng
- `.promotion-meta`: Thông tin meta (tác giả)
- `.btn-read-more`: Nút xem chi tiết

## Cách sử dụng

### 1. Quản lý Tin tức (Admin)

1. Truy cập `/Admin/News`
2. Tạo tin tức mới:
   - **Loại tin tức**: Chọn "Tin tức" hoặc "Khuyến mãi"
   - **Tiêu đề**: Tiêu đề tin tức/khuyến mãi
   - **Mô tả ngắn**: Hiển thị trong danh sách
   - **Nội dung**: Nội dung chi tiết (hỗ trợ HTML)
   - **Hình ảnh**: Upload hình ảnh đại diện
   - **Trạng thái**: Đánh dấu "Đã xuất bản" để hiển thị

### 2. Phân loại tự động

- **NewsType = "News"**: Hiển thị tại `/News` và trang chủ
- **NewsType = "Promotion"**: Hiển thị tại `/Promotion`

### 3. Hiển thị trên Frontend

**Trang chủ (`/`)**:
- Hiển thị 4 tin tức mới nhất (NewsType = "News")

**Trang tin tức (`/News`)**:
- Hiển thị tất cả tin tức đã xuất bản
- Chức năng tìm kiếm
- Modal xem chi tiết

**Trang khuyến mãi (`/Promotion`)**:
- Hiển thị tất cả khuyến mãi đã xuất bản
- Card với hình ảnh và thông tin cơ bản
- Modal xem chi tiết khuyến mãi

## API Endpoints

### News
- `GET /News` - Trang danh sách tin tức
- `GET /News/GetNewsDetails/{id}` - API lấy chi tiết tin tức

### Promotions
- `GET /Promotion` - Trang danh sách khuyến mãi
- `GET /Promotion/GetPromotionDetails/{id}` - API lấy chi tiết khuyến mãi
- `POST /Promotion/ValidateCouponCode` - API validate mã giảm giá (tương thích cũ)

## Lưu ý quan trọng

1. **Tương thích ngược**: API `ValidateCouponCode` vẫn sử dụng bảng `Promotions` cũ để tương thích với hệ thống giỏ hàng hiện tại.

2. **Phân quyền**: Chỉ admin mới có thể tạo/sửa/xóa tin tức và khuyến mãi.

3. **SEO Friendly**: Các URL được thiết kế thân thiện với SEO.

4. **Responsive**: Giao diện tương thích với mobile và desktop.

5. **Performance**: Sử dụng pagination và lazy loading cho danh sách lớn.

## Troubleshooting

### Không hiển thị tin tức/khuyến mãi
- Kiểm tra `IsPublished = true`
- Kiểm tra `NewsType` đúng ("News" hoặc "Promotion")
- Kiểm tra ngày xuất bản

### Lỗi hiển thị hình ảnh
- Kiểm tra đường dẫn hình ảnh trong database
- Đảm bảo file hình ảnh tồn tại trong thư mục `wwwroot/uploads/`

### API không hoạt động
- Kiểm tra controller và action method
- Kiểm tra CORS settings nếu cần
- Kiểm tra database connection

## Kết luận

Việc tích hợp này giúp:
- Quản lý tập trung tin tức và khuyến mãi từ admin
- Giảm thiểu việc hardcode dữ liệu
- Tăng tính linh hoạt trong việc cập nhật nội dung
- Cải thiện trải nghiệm người dùng với giao diện hiện đại 