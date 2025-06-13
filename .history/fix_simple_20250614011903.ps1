# Script đơn giản để sửa lỗi encoding
$files = Get-ChildItem -Path "Areas/Admin" -Include "*.cs", "*.cshtml" -Recurse

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $originalContent = $content
    
    # Sửa một số lỗi cơ bản
    $content = $content -replace "má»›i", "mới"
    $content = $content -replace "ThÃªm", "Thêm"
    $content = $content -replace "ngÆ°á»i dÃ¹ng", "người dùng"
    $content = $content -replace "cá»­a hÃ ng", "cửa hàng"
    $content = $content -replace "dá»‹ch vá»¥", "dịch vụ"
    $content = $content -replace "tin tá»©c", "tin tức"
    $content = $content -replace "sáº£n pháº©m", "sản phẩm"
    $content = $content -replace "danh má»¥c", "danh mục"
    $content = $content -replace "vá»›i", "với"
    $content = $content -replace "nÃºt", "nút"
    
    if ($content -ne $originalContent) {
        Write-Host "Fixing: $($file.Name)"
        [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.Encoding]::UTF8)
    }
}

Write-Host "Done!" 