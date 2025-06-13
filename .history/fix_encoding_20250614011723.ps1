# Script để sửa lỗi encoding tiếng Việt
$encodingFixes = @{
    "NgÆ°á»i dÃ¹ng má»›i" = "Người dùng mới"
    "ThÃªm ngÆ°á»i dÃ¹ng má»›i" = "Thêm người dùng mới"
    "má»›i" = "mới"
    "ThÃªm" = "Thêm"
    "ngÆ°á»i dÃ¹ng" = "người dùng"
    "cá»­a hÃ ng" = "cửa hàng"
    "ThÃªm cá»­a hÃ ng má»›i" = "Thêm cửa hàng mới"
    "dá»‹ch vá»¥" = "dịch vụ"
    "ThÃªm dá»‹ch vá»¥ má»›i" = "Thêm dịch vụ mới"
    "tin tá»©c" = "tin tức"
    "ThÃªm tin tá»©c má»›i" = "Thêm tin tức mới"
    "sáº£n pháº©m" = "sản phẩm"
    "ThÃªm sáº£n pháº©m má»›i" = "Thêm sản phẩm mới"
    "danh má»¥c" = "danh mục"
    "ThÃªm danh má»¥c má»›i" = "Thêm danh mục mới"
    "vá»›i" = "với"
    "nÃºt" = "nút"
    "Hiá»ƒn thá»‹" = "Hiển thị"
    "thÃ´ng bÃ¡o" = "thông báo"
    "TÃ¬m kiáº¿m" = "Tìm kiếm"
    "tÃªn" = "tên"
    "email" = "email"
    "sá»'" = "số"
    "Ä'iá»‡n thoáº¡i" = "điện thoại"
    "Táº¥t cáº£" = "Tất cả"
    "tráº¡ng thÃ¡i" = "trạng thái"
    "Äang hoáº¡t Ä'á»™ng" = "Đang hoạt động"
    "ÄÃ£ vÃ´ hiá»‡u hÃ³a" = "Đã vô hiệu hóa"
    "Lá»c" = "Lọc"
    "XÃ³a bá»™ lá»c" = "Xóa bộ lọc"
    "Dáº¡ng lÆ°á»›i" = "Dạng lưới"
    "Dáº¡ng danh sÃ¡ch" = "Dạng danh sách"
    "Hiá»ƒn thá»‹" = "Hiển thị"
    "káº¿t quáº£" = "kết quả"
    "Trang" = "Trang"
    "Danh sÃ¡ch" = "Danh sách"
    "chi tiáº¿t" = "chi tiết"
    "Chá»‰nh sá»­a" = "Chỉnh sửa"
    "XÃ³a" = "Xóa"
    "ChÆ°a cÃ³ SÄT" = "Chưa có SĐT"
    "Äiá»n thÃ´ng tin" = "Điền thông tin"
    "há»‡ thá»'ng" = "hệ thống"
    "Táº¡o" = "Tạo"
    "khuyáº¿n mÃ£i" = "khuyến mãi"
    "bÃ i viáº¿t" = "bài viết"
    "hoáº·c" = "hoặc"
    "áº£nh" = "ảnh"
    "hiá»‡n táº¡i" = "hiện tại"
    "upload" = "upload"
    "voucher" = "voucher"
    "giáº£m giÃ¡" = "giảm giá"
    "ÄÆ¡n hÃ ng" = "Đơn hàng"
    "Má»›i nháº¥t" = "Mới nhất"
    "NgÃ y má»›i nháº¥t" = "Ngày mới nhất"
    "LÃ m má»›i" = "Làm mới"
    "Chá»n" = "Chọn"
    "Vui lÃ²ng" = "Vui lòng"
    "cho" = "cho"
    "Header" = "Header"
    "Thá»'ng kÃª" = "Thống kê"
    "hÃ´m nay" = "hôm nay"
    "estimate" = "estimate"
    "based" = "based"
    "recent" = "recent"
    "activity" = "activity"
    "tuáº§n nÃ y" = "tuần này"
    "KhÃ´ng thá»ƒ" = "Không thể"
    "chuyá»ƒn" = "chuyển"
    "tá»«" = "từ"
    "sang" = "sang"
    "xÃ¡c nháº­n" = "xác nhận"
    "Chá»" = "Chờ"
    "Ä'Ã£" = "đã"
    "Ä'Æ°á»£c" = "được"
    "táº¡o" = "tạo"
    "Ä'Äƒng kÃ½" = "đăng ký"
    "phÃºt" = "phút"
    "trÆ°á»›c" = "trước"
    "ÄÃ¡nh giÃ¡" = "Đánh giá"
    "sao" = "sao"
    "vÃ o" = "vào"
    "Máº­t kháº©u" = "Mật khẩu"
}

# Lấy tất cả files .cs và .cshtml trong Areas/Admin
$files = Get-ChildItem -Path "Areas/Admin" -Include "*.cs", "*.cshtml" -Recurse

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $originalContent = $content
    
    foreach ($pair in $encodingFixes.GetEnumerator()) {
        $content = $content -replace [regex]::Escape($pair.Key), $pair.Value
    }
    
    if ($content -ne $originalContent) {
        Write-Host "Fixing encoding in: $($file.FullName)"
        [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.Encoding]::UTF8)
    }
}

Write-Host "Encoding fix completed!" 