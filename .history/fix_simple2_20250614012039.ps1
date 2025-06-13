# Script 2 để sửa thêm lỗi encoding
$files = Get-ChildItem -Path "Areas/Admin" -Include "*.cs", "*.cshtml" -Recurse

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $originalContent = $content
    
    # Sửa thêm các lỗi khác
    $content = $content -replace "Hiá»ƒn thá»‹", "Hiển thị"
    $content = $content -replace "thÃ´ng bÃ¡o", "thông báo"
    $content = $content -replace "TÃ¬m kiáº¿m", "Tìm kiếm"
    $content = $content -replace "tÃªn", "tên"
    $content = $content -replace "tráº¡ng thÃ¡i", "trạng thái"
    $content = $content -replace "Äang hoáº¡t Ä'á»™ng", "Đang hoạt động"
    $content = $content -replace "ÄÃ£ vÃ´ hiá»‡u hÃ³a", "Đã vô hiệu hóa"
    $content = $content -replace "Lá»c", "Lọc"
    $content = $content -replace "XÃ³a", "Xóa"
    $content = $content -replace "bá»™ lá»c", "bộ lọc"
    $content = $content -replace "chi tiáº¿t", "chi tiết"
    $content = $content -replace "Chá»‰nh sá»­a", "Chỉnh sửa"
    $content = $content -replace "Táº¥t cáº£", "Tất cả"
    $content = $content -replace "Má»›i nháº¥t", "Mới nhất"
    $content = $content -replace "LÃ m má»›i", "Làm mới"
    
    if ($content -ne $originalContent) {
        Write-Host "Fixing: $($file.Name)"
        [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.Encoding]::UTF8)
    }
}

Write-Host "Done!" 