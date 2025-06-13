# Encoding fix script - Comprehensive solution
$ErrorActionPreference = "Continue"

# Define all Vietnamese encoding fixes
$fixes = @{
    # Basic words
    "má»›i" = "mới"
    "ThÃªm" = "Thêm"
    "ngÆ°á»i dÃ¹ng" = "người dùng"
    "cá»­a hÃ ng" = "cửa hàng"
    "dá»‹ch vá»¥" = "dịch vụ"
    "tin tá»©c" = "tin tức"
    "sáº£n pháº©m" = "sản phẩm"
    "danh má»¥c" = "danh mục"
    "vá»›i" = "với"
    "nÃºt" = "nút"
    
    # Interface text
    "Hiá»ƒn thá»‹" = "Hiển thị"
    "thÃ´ng bÃ¡o" = "thông báo"
    "TÃ¬m kiáº¿m" = "Tìm kiếm"
    "tÃªn" = "tên"
    "tráº¡ng thÃ¡i" = "trạng thái"
    "Äang hoáº¡t Ä'á»™ng" = "Đang hoạt động"
    "ÄÃ£ vÃ´ hiá»‡u hÃ³a" = "Đã vô hiệu hóa"
    "Lá»c" = "Lọc"
    "XÃ³a" = "Xóa"
    "bá»™ lá»c" = "bộ lọc"
    "chi tiáº¿t" = "chi tiết"
    "Chá»‰nh sá»­a" = "Chỉnh sửa"
    "Táº¥t cáº£" = "Tất cả"
    "Má»›i nháº¥t" = "Mới nhất"
    "LÃ m má»›i" = "Làm mới"
    
    # Form fields
    "Äiá»n thÃ´ng tin" = "Điền thông tin"
    "há»‡ thá»'ng" = "hệ thống"
    "Táº¡o" = "Tạo"
    "khuyáº¿n mÃ£i" = "khuyến mãi"
    "bÃ i viáº¿t" = "bài viết"
    "hoáº·c" = "hoặc"
    "áº£nh" = "ảnh"
    "hiá»‡n táº¡i" = "hiện tại"
    "giáº£m giÃ¡" = "giảm giá"
    "ÄÆ¡n hÃ ng" = "Đơn hàng"
    "NgÃ y má»›i nháº¥t" = "Ngày mới nhất"
    "Chá»n" = "Chọn"
    "Vui lÃ²ng" = "Vui lòng"
    "Máº­t kháº©u" = "Mật khẩu"
    
    # Status and actions
    "ChÆ°a cÃ³ SÄT" = "Chưa có SĐT"
    "sá»'" = "số"
    "Ä'iá»‡n thoáº¡i" = "điện thoại"
    "Dáº¡ng lÆ°á»›i" = "Dạng lưới"
    "Dáº¡ng danh sÃ¡ch" = "Dạng danh sách"
    "káº¿t quáº£" = "kết quả"
    "Trang" = "Trang"
    "Danh sÃ¡ch" = "Danh sách"
    
    # Comments and descriptions  
    "Thá»'ng kÃª" = "Thống kê"
    "hÃ´m nay" = "hôm nay"
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
    
    # Specific phrases
    "NgÆ°á»i dÃ¹ng má»›i" = "Người dùng mới"
    "ThÃªm ngÆ°á»i dÃ¹ng má»›i" = "Thêm người dùng mới"
    "ThÃªm cá»­a hÃ ng má»›i" = "Thêm cửa hàng mới"
    "ThÃªm dá»‹ch vá»¥ má»›i" = "Thêm dịch vụ mới"
    "ThÃªm tin tá»©c má»›i" = "Thêm tin tức mới"
    "ThÃªm sáº£n pháº©m má»›i" = "Thêm sản phẩm mới"
    "ThÃªm danh má»¥c má»›i" = "Thêm danh mục mới"
    "Táº¡o voucher má»›i" = "Tạo voucher mới"
    "Táº¡o Voucher má»›i" = "Tạo Voucher mới"
    "Header vá»›i nÃºt thÃªm má»›i" = "Header với nút thêm mới"
    "XÃ³a bá»™ lá»c" = "Xóa bộ lọc"
    "TÃ¬m kiáº¿m tÃªn, email, sá»' Ä'iá»‡n thoáº¡i" = "Tìm kiếm tên, email, số điện thoại"
    "cá»§a" = "của"
    "Upload áº£nh má»›i" = "Upload ảnh mới"
    "áº¢nh má»›i" = "Ảnh mới"
    "XÃ³a áº£nh má»›i" = "Xóa ảnh mới"
    "Tráº¡ng thÃ¡i má»›i" = "Trạng thái mới"
    "Chá»n tráº¡ng thÃ¡i má»›i" = "Chọn trạng thái mới"
    "áº¢nh hiá»‡n táº¡i vÃ " = "Ảnh hiện tại và"
}

Write-Host "Starting comprehensive encoding fix..." -ForegroundColor Green

# Get all relevant files
$files = @()
$files += Get-ChildItem -Path "Areas/Admin" -Include "*.cs", "*.cshtml" -Recurse
$files += Get-ChildItem -Path "Views" -Include "*.cshtml" -Recurse -ErrorAction SilentlyContinue
$files += Get-ChildItem -Path "Controllers" -Include "*.cs" -Recurse -ErrorAction SilentlyContinue

$totalFiles = $files.Count
$processedFiles = 0
$fixedFiles = 0

foreach ($file in $files) {
    $processedFiles++
    Write-Progress -Activity "Fixing encoding" -Status "Processing $($file.Name)" -PercentComplete (($processedFiles / $totalFiles) * 100)
    
    try {
        $content = Get-Content $file.FullName -Raw -Encoding UTF8
        $originalContent = $content
        $fileFixed = $false
        
        foreach ($fix in $fixes.GetEnumerator()) {
            if ($content.Contains($fix.Key)) {
                $content = $content.Replace($fix.Key, $fix.Value)
                $fileFixed = $true
            }
        }
        
        if ($fileFixed) {
            [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.Encoding]::UTF8)
            $fixedFiles++
            Write-Host "Fixed: $($file.FullName)" -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host "Error processing $($file.FullName): $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`nEncoding fix completed!" -ForegroundColor Green
Write-Host "Processed: $processedFiles files" -ForegroundColor Cyan
Write-Host "Fixed: $fixedFiles files" -ForegroundColor Cyan
Write-Host "Vietnamese text should now display correctly!" -ForegroundColor Green 