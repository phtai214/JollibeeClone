# Simple encoding fix - Safe version
$ErrorActionPreference = "Continue"
Write-Host "Starting encoding fix..." -ForegroundColor Green

# Get all files
$files = @()
$files += Get-ChildItem -Path "Areas/Admin" -Include "*.cs", "*.cshtml" -Recurse
$files += Get-ChildItem -Path "Views" -Include "*.cshtml" -Recurse -ErrorAction SilentlyContinue  
$files += Get-ChildItem -Path "Controllers" -Include "*.cs" -Recurse -ErrorAction SilentlyContinue

$processedFiles = 0
$fixedFiles = 0

foreach ($file in $files) {
    $processedFiles++
    Write-Host "Processing: $($file.Name)" -ForegroundColor Cyan
    
    try {
        $content = Get-Content $file.FullName -Raw -Encoding UTF8
        $originalContent = $content
        $fileFixed = $false
        
        # Apply fixes using simple replacements
        $replacements = @(
            @("má»›i", "mới"),
            @("ThÃªm", "Thêm"),
            @("ngÆ°á»i", "người"),
            @("dÃ¹ng", "dùng"),
            @("cá»­a", "cửa"),
            @("hÃ ng", "hàng"),
            @("dá»‹ch", "dịch"),
            @("vá»¥", "vụ"),
            @("tin", "tin"),
            @("tá»©c", "tức"),
            @("sáº£n", "sản"),
            @("pháº©m", "phẩm"),
            @("danh", "danh"),
            @("má»¥c", "mục"),
            @("vá»›i", "với"),
            @("nÃºt", "nút"),
            @("Hiá»ƒn", "Hiển"),
            @("thá»‹", "thị"),
            @("thÃ´ng", "thông"),
            @("bÃ¡o", "báo"),
            @("TÃ¬m", "Tìm"),
            @("kiáº¿m", "kiếm"),
            @("tÃªn", "tên"),
            @("tráº¡ng", "trạng"),
            @("thÃ¡i", "thái"),
            @("Äang", "Đang"),
            @("hoáº¡t", "hoạt"),
            @("Ä'á»™ng", "động"),
            @("ÄÃ£", "Đã"),
            @("vÃ´", "vô"),
            @("hiá»‡u", "hiệu"),
            @("hÃ³a", "hóa"),
            @("Lá»c", "Lọc"),
            @("XÃ³a", "Xóa"),
            @("bá»™", "bộ"),
            @("lá»c", "lọc"),
            @("chi", "chi"),
            @("tiáº¿t", "tiết"),
            @("Chá»‰nh", "Chỉnh"),
            @("sá»­a", "sửa"),
            @("Táº¥t", "Tất"),
            @("cáº£", "cả"),
            @("Má»›i", "Mới"),
            @("nháº¥t", "nhất"),
            @("LÃ m", "Làm"),
            @("Äiá»n", "Điền"),
            @("há»‡", "hệ"),
            @("thá»'ng", "thống"),
            @("Táº¡o", "Tạo"),
            @("khuyáº¿n", "khuyến"),
            @("mÃ£i", "mãi"),
            @("bÃ i", "bài"),
            @("viáº¿t", "viết"),
            @("hoáº·c", "hoặc"),
            @("áº£nh", "ảnh"),
            @("hiá»‡n", "hiện"),
            @("táº¡i", "tại"),
            @("giáº£m", "giảm"),
            @("giÃ¡", "giá"),
            @("ÄÆ¡n", "Đơn"),
            @("hÃ ng", "hàng"),
            @("NgÃ y", "Ngày"),
            @("Chá»n", "Chọn"),
            @("Vui", "Vui"),
            @("lÃ²ng", "lòng"),
            @("Máº­t", "Mật"),
            @("kháº©u", "khẩu"),
            @("ChÆ°a", "Chưa"),
            @("cÃ³", "có"),
            @("sá»'", "số"),
            @("Ä'iá»‡n", "điện"),
            @("thoáº¡i", "thoại"),
            @("Dáº¡ng", "Dạng"),
            @("lÆ°á»›i", "lưới"),
            @("sÃ¡ch", "sách"),
            @("káº¿t", "kết"),
            @("quáº£", "quả"),
            @("Trang", "Trang"),
            @("Danh", "Danh"),
            @("hÃ´m", "hôm"),
            @("nay", "nay"),
            @("tuáº§n", "tuần"),
            @("nÃ y", "này"),
            @("KhÃ´ng", "Không"),
            @("thá»ƒ", "thể"),
            @("chuyá»ƒn", "chuyển"),
            @("tá»«", "từ"),
            @("xÃ¡c", "xác"),
            @("nháº­n", "nhận"),
            @("Chá»", "Chờ"),
            @("Ä'Æ°á»£c", "được"),
            @("Ä'Äƒng", "đăng"),
            @("kÃ½", "ký"),
            @("phÃºt", "phút"),
            @("trÆ°á»›c", "trước"),
            @("ÄÃ¡nh", "Đánh"),
            @("giÃ¡", "giá"),
            @("vÃ o", "vào"),
            @("cá»§a", "của"),
            @("Upload", "Upload"),
            @("áº¢nh", "Ảnh")
        )
        
        foreach ($replacement in $replacements) {
            $oldText = $replacement[0]
            $newText = $replacement[1]
            if ($content.Contains($oldText)) {
                $content = $content.Replace($oldText, $newText)
                $fileFixed = $true
            }
        }
        
        if ($fileFixed) {
            [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.Encoding]::UTF8)
            $fixedFiles++
            Write-Host "Fixed: $($file.Name)" -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host "Error: $($file.Name) - $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Encoding fix completed!" -ForegroundColor Green
Write-Host "Processed: $processedFiles files" -ForegroundColor Cyan  
Write-Host "Fixed: $fixedFiles files" -ForegroundColor Cyan 