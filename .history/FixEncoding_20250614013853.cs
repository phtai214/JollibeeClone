using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

class FixEncodingProgram
{
    static void Main()
    {
        var fixes = new Dictionary<string, string>
        {
            // Basic patterns
            {"má»›i", "mới"},
            {"ThÃªm", "Thêm"},
            {"ngÆ°á»i", "người"},
            {"dÃ¹ng", "dùng"},
            {"cá»­a", "cửa"},
            {"hÃ ng", "hàng"},
            {"dá»‹ch", "dịch"},
            {"vá»¥", "vụ"},
            {"tin", "tin"},
            {"tá»©c", "tức"},
            {"sáº£n", "sản"},
            {"pháº©m", "phẩm"},
            {"danh", "danh"},
            {"má»¥c", "mục"},
            {"vá»›i", "với"},
            {"nÃºt", "nút"},
            
            // Interface text
            {"Hiá»ƒn", "Hiển"},
            {"thá»‹", "thị"},
            {"thÃ´ng", "thông"},
            {"bÃ¡o", "báo"},
            {"TÃ¬m", "Tìm"},
            {"kiáº¿m", "kiếm"},
            {"tÃªn", "tên"},
            {"tráº¡ng", "trạng"},
            {"thÃ¡i", "thái"},
            {"Äang", "Đang"},
            {"hoáº¡t", "hoạt"},
            {"Ä'á»™ng", "động"},
            {"ÄÃ£", "Đã"},
            {"vÃ´", "vô"},
            {"hiá»‡u", "hiệu"},
            {"hÃ³a", "hóa"},
            {"Lá»c", "Lọc"},
            {"XÃ³a", "Xóa"},
            {"bá»™", "bộ"},
            {"lá»c", "lọc"},
            {"chi", "chi"},
            {"tiáº¿t", "tiết"},
            {"Chá»‰nh", "Chỉnh"},
            {"sá»­a", "sửa"},
            {"Táº¥t", "Tất"},
            {"cáº£", "cả"},
            {"Má»›i", "Mới"},
            {"nháº¥t", "nhất"},
            {"LÃ m", "Làm"},
            
            // Form fields
            {"Äiá»n", "Điền"},
            {"há»‡", "hệ"},
            {"thá»'ng", "thống"},
            {"Táº¡o", "Tạo"},
            {"khuyáº¿n", "khuyến"},
            {"mÃ£i", "mãi"},
            {"bÃ i", "bài"},
            {"viáº¿t", "viết"},
            {"hoáº·c", "hoặc"},
            {"áº£nh", "ảnh"},
            {"hiá»‡n", "hiện"},
            {"táº¡i", "tại"},
            {"giáº£m", "giảm"},
            {"giÃ¡", "giá"},
            {"ÄÆ¡n", "Đơn"},
            {"NgÃ y", "Ngày"},
            {"Chá»n", "Chọn"},
            {"Vui", "Vui"},
            {"lÃ²ng", "lòng"},
            {"Máº­t", "Mật"},
            {"kháº©u", "khẩu"},
            
            // Status
            {"ChÆ°a", "Chưa"},
            {"cÃ³", "có"},
            {"sá»'", "số"},
            {"Ä'iá»‡n", "điện"},
            {"thoáº¡i", "thoại"},
            {"Dáº¡ng", "Dạng"},
            {"lÆ°á»›i", "lưới"},
            {"sÃ¡ch", "sách"},
            {"káº¿t", "kết"},
            {"quáº£", "quả"},
            {"Trang", "Trang"},
            {"Danh", "Danh"},
            {"hÃ´m", "hôm"},
            {"nay", "nay"},
            {"tuáº§n", "tuần"},
            {"nÃ y", "này"},
            {"KhÃ´ng", "Không"},
            {"thá»ƒ", "thể"},
            {"chuyá»ƒn", "chuyển"},
            {"tá»«", "từ"},
            {"xÃ¡c", "xác"},
            {"nháº­n", "nhận"},
            {"Chá»", "Chờ"},
            {"Ä'Æ°á»£c", "được"},
            {"Ä'Äƒng", "đăng"},
            {"kÃ½", "ký"},
            {"phÃºt", "phút"},
            {"trÆ°á»›c", "trước"},
            {"ÄÃ¡nh", "Đánh"},
            {"vÃ o", "vào"},
            {"cá»§a", "của"},
            {"áº¢nh", "Ảnh"},
            
            // Common phrases
            {"ngÆ°á»i dÃ¹ng", "người dùng"},
            {"cá»­a hÃ ng", "cửa hàng"},
            {"dá»‹ch vá»¥", "dịch vụ"},
            {"tin tá»©c", "tin tức"},
            {"sáº£n pháº©m", "sản phẩm"},
            {"danh má»¥c", "danh mục"},
            {"tráº¡ng thÃ¡i", "trạng thái"},
            {"bá»™ lá»c", "bộ lọc"},
            {"chi tiáº¿t", "chi tiết"},
            {"ChÆ°a cÃ³ SÄT", "Chưa có SĐT"},
            {"Ä'iá»‡n thoáº¡i", "điện thoại"},
            {"thÃ´ng bÃ¡o", "thông báo"},
            {"Hiá»ƒn thá»‹", "Hiển thị"},
            {"TÃ¬m kiáº¿m", "Tìm kiếm"},
            {"Äang hoáº¡t Ä'á»™ng", "Đang hoạt động"},
            {"ÄÃ£ vÃ´ hiá»‡u hÃ³a", "Đã vô hiệu hóa"},
            {"Chá»‰nh sá»­a", "Chỉnh sửa"},
            {"Táº¥t cáº£", "Tất cả"},
            {"Má»›i nháº¥t", "Mới nhất"},
            {"Dáº¡ng lÆ°á»›i", "Dạng lưới"},
            {"Dáº¡ng danh sÃ¡ch", "Dạng danh sách"}
        };

        var directories = new[] { "Areas\\Admin" };
        var extensions = new[] { "*.cs", "*.cshtml" };
        
        int processedFiles = 0;
        int fixedFiles = 0;

        Console.WriteLine("Starting encoding fix...");

        foreach (var directory in directories)
        {
            if (!Directory.Exists(directory)) continue;

            foreach (var extension in extensions)
            {
                var files = Directory.GetFiles(directory, extension, SearchOption.AllDirectories);
                
                foreach (var file in files)
                {
                    processedFiles++;
                    Console.WriteLine($"Processing: {Path.GetFileName(file)}");
                    
                    try
                    {
                        var content = File.ReadAllText(file, Encoding.UTF8);
                        var originalContent = content;
                        var fileFixed = false;

                        foreach (var fix in fixes)
                        {
                            if (content.Contains(fix.Key))
                            {
                                content = content.Replace(fix.Key, fix.Value);
                                fileFixed = true;
                            }
                        }

                        if (fileFixed)
                        {
                            File.WriteAllText(file, content, new UTF8Encoding(false));
                            fixedFiles++;
                            Console.WriteLine($"Fixed: {Path.GetFileName(file)}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing {file}: {ex.Message}");
                    }
                }
            }
        }

        Console.WriteLine();
        Console.WriteLine("Encoding fix completed!");
        Console.WriteLine($"Processed: {processedFiles} files");
        Console.WriteLine($"Fixed: {fixedFiles} files");
        Console.WriteLine("Vietnamese text should now display correctly!");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
} 