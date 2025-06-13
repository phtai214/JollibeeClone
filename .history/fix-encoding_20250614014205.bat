@echo off
chcp 65001 >nul
echo Fixing Vietnamese encoding...

powershell -Command "& {
    $files = Get-ChildItem 'Areas\Admin' -Include '*.cs', '*.cshtml' -Recurse
    $fixed = 0
    foreach ($file in $files) {
        $content = [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8)
        $original = $content
        
        # Basic fixes
        $content = $content -replace 'má»›i', 'mới'
        $content = $content -replace 'ngÆ°á»i dÃ¹ng', 'người dùng'
        $content = $content -replace 'TÃ¬m kiáº¿m', 'Tìm kiếm'
        $content = $content -replace 'tráº¡ng thÃ¡i', 'trạng thái'
        $content = $content -replace 'Hiá»ƒn thá»‹', 'Hiển thị'
        $content = $content -replace 'thÃ´ng bÃ¡o', 'thông báo'
        $content = $content -replace 'chi tiáº¿t', 'chi tiết'
        $content = $content -replace 'Chá»‰nh sá»­a', 'Chỉnh sửa'
        $content = $content -replace 'XÃ³a', 'Xóa'
        $content = $content -replace 'bá»™ lá»c', 'bộ lọc'
        $content = $content -replace 'Táº¥t cáº£', 'Tất cả'
        $content = $content -replace 'Äang hoáº¡t Ä''á»™ng', 'Đang hoạt động'
        $content = $content -replace 'ÄÃ£ vÃ´ hiá»‡u hÃ³a', 'Đã vô hiệu hóa'
        $content = $content -replace 'tÃªn', 'tên'
        $content = $content -replace 'Lá»c', 'Lọc'
        $content = $content -replace 'Dáº¡ng lÆ°á»›i', 'Dạng lưới'
        $content = $content -replace 'Dáº¡ng danh sÃ¡ch', 'Dạng danh sách'
        $content = $content -replace 'ChÆ°a cÃ³ SÄT', 'Chưa có SĐT'
        $content = $content -replace 'Ä''iá»‡n thoáº¡i', 'điện thoại'
        $content = $content -replace 'cá»§a', 'của'
        $content = $content -replace 'Danh sÃ¡ch', 'Danh sách'
        $content = $content -replace 'káº¿t quáº£', 'kết quả'
        $content = $content -replace 'sá»'', 'số'
        $content = $content -replace 'Ä''á»‡n thoáº¡i', 'điện thoại'
        $content = $content -replace 'vá»›i', 'với'
        $content = $content -replace 'nÃºt', 'nút'
        $content = $content -replace 'sáº£n pháº©m', 'sản phẩm'
        $content = $content -replace 'danh má»¥c', 'danh mục'
        $content = $content -replace 'cá»­a hÃ ng', 'cửa hàng'
        $content = $content -replace 'dá»‹ch vá»¥', 'dịch vụ'
        $content = $content -replace 'tin tá»©c', 'tin tức'
        
        if ($content -ne $original) {
            [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.UTF8Encoding]::new($false))
            Write-Host \"Fixed: $($file.Name)\" -ForegroundColor Yellow
            $fixed++
        }
    }
    Write-Host \"\"
    Write-Host \"Completed! Fixed $fixed files\" -ForegroundColor Green
}"

echo.
echo Done! Press any key to continue...
pause >nul 