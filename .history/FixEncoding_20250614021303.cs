using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

class FixEncodingProgram
{
    static void Main()
    {
        // Dictionary chứa tất cả các lỗi encoding phổ biến
        var encodingFixes = new Dictionary<string, string>
        {
            // Basic encoding fixes
            {"Náº¿u", "Nếu"},
            {"Ä'Ã£", "đã"},
            {"Ä'Äƒng nháº­p", "đăng nhập"},
            {"thÃ¬", "thì"},
            {"chuyá»ƒn", "chuyển"},
            {"vá»", "về"},
            {"TÃ¬m", "Tìm"},
            {"ngÆ°á»i dÃ¹ng", "người dùng"},
            {"hoáº·c", "hoặc"},
            {"máº­t kháº©u", "mật khẩu"},
            {"khÃ´ng", "không"},
            {"chÃ­nh xÃ¡c", "chính xác"},
            {"Kiá»ƒm tra", "Kiểm tra"},
            {"quyá»n", "quyền"},
            {"truy cáº­p", "truy cập"},
            {"vÃ o", "vào"},
            {"khu vá»±c", "khu vực"},
            {"quáº£n trá»‹", "quản trị"},
            {"LÆ°u", "Lưu"},
            {"thÃ´ng tin", "thông tin"},
            {"CÃ³ thá»ƒ", "Có thể"},
            {"lÆ°u", "lưu"},
            {"á»Ÿ", "ở"},
            {"Ä'Ã¢y", "đây"},
            {"náº¿u", "nếu"},
            {"cáº§n", "cần"},
            {"thÃ nh cÃ´ng", "thành công"},
            {"CÃ³ lá»—i", "Có lỗi"},
            {"xáº£y ra", "xảy ra"},
            {"trong quÃ¡ trÃ¬nh", "trong quá trình"},
            {"Vui lÃ²ng", "Vui lòng"},
            {"thá»­ láº¡i", "thử lại"},
            {"XÃ³a", "Xóa"},
            {"Ä'Äƒng xuáº¥t", "đăng xuất"},
            {"Ä'á»ƒ", "để"},
            {"táº¡o", "tạo"},
            {"Ä'áº§u tiÃªn", "đầu tiên"},
            {"chá»‰", "chỉ"},
            {"dÃ¹ng", "dùng"},
            {"xem", "xem"},
            {"chÆ°a", "chưa"},
            {"Máº­t kháº©u", "Mật khẩu"},
            {"máº·c Ä'á»‹nh", "mặc định"},
            {"GÃ¡n", "Gán"},
            {"tá»"n táº¡i", "tồn tại"},
            {"há»£p lá»‡", "hợp lệ"},
            {"KhÃ´ng", "Không"},
            {"tÃ¬m tháº¥y", "tìm thấy"},
            {"táº£i", "tải"},
            {"danh sÃ¡ch", "danh sách"},
            {"danh má»¥c", "danh mục"},
            {"sáº£n pháº©m", "sản phẩm"},
            {"Ä'Æ¡n hÃ ng", "đơn hàng"},
            {"dá»‹ch vá»¥", "dịch vụ"},
            {"tin tá»©c", "tin tức"},
            {"cáº­p nháº­t", "cập nhật"},
            {"chá»‰nh sá»­a", "chỉnh sửa"},
            {"pháº£i", "phải"},
            {"cÃ³", "có"},
            {"Ã­t nháº¥t", "ít nhất"},
            {"kÃ½ tá»±", "ký tự"},
            {"vÃ¬", "vì"},
            {"Ä'Æ°á»£c", "được"},
            {"vÃ´ hiá»‡u hÃ³a", "vô hiệu hóa"},
            {"khá»›p", "khớp"},
            {"Dá»¯ liá»‡u", "Dữ liệu"},
            {"báº¡n", "bạn"},
            {"Báº¡n", "Bạn"},
            {"má»›i", "mới"},
            {"má»", "mở"},
            {"cá»­a", "cửa"},
            {"Táº¥t cáº£", "Tất cả"},
            {"loáº¡i", "loại"},
            {"Tin tá»©c", "Tin tức"},
            {"Khuyáº¿n mÃ£i", "Khuyến mãi"},
            {"TiÃªu Ä'á»", "Tiêu đề"},
            {"má»™t", "một"},
            {"sá»'", "số"},
            {"hoáº¡t Ä'á»™ng", "hoạt động"},
            {"giáº£ láº­p", "giả lập"},
            {"sá»­ dá»¥ng", "sử dụng"},
            {"ÄÃ¡nh giÃ¡", "Đánh giá"},
            {"phÃºt trÆ°á»›c", "phút trước"},
            {"giá» trÆ°á»›c", "giờ trước"},
            {"ngÃ y trÆ°á»›c", "ngày trước"},
            {"vá»«a xong", "vừa xong"},
            {"vÃ i phÃºt trÆ°á»›c", "vài phút trước"},
            {"gáº§n Ä'Ã¢y", "gần đây"},
            {"Ä'á»ng cá»­a", "đóng cửa"},
            {"Má»Ÿ cá»­a", "Mở cửa"},
            {"Táº¡m khÃ³a", "Tạm khóa"},
            {"Hoáº¡t Ä'á»™ng", "Hoạt động"},
            {"táº¥t cáº£", "tất cả"},
            {"Quáº£n lÃ½", "Quản lý"},
            {"vÃ ", "và"},
            {"Thêm", "Thêm"},
            {"há»‡ thá»'ng", "hệ thống"},
            {"Táº¡o", "Tạo"},
            {"Láº¥y", "Lấy"},
            {"thá»'ng kÃª", "thống kê"},
            {"cÆ¡ báº£n", "cơ bản"},
            {"TÃ­nh", "Tính"},
            {"tuáº§n nÃ y", "tuần này"},
            {"tuáº§n", "tuần"},
            {"hÃ´m nay", "hôm nay"},
            {"nÃ y", "này"},
            {"KhÃ´ng thá»ƒ", "Không thể"},
            {"xÃ³a", "xóa"},
            {"Giáº£ sá»­", "Giả sử"},
            {"sá»­ dá»¥ng", "sử dụng"},
            {"SHA256", "SHA256"},
            {"bcrypt", "bcrypt"},
            {"ÄÃ¢y lÃ ", "Đây là"},
            {"vÃ­ dá»¥", "ví dụ"},
            {"Ä'Æ¡n giáº£n", "đơn giản"},
            {"vá»›i", "với"},
            {"Action", "Action"},
            {"development", "development"},
            {"Administrator", "Administrator"},
            {"Email", "Email"},
            {"role", "role"},
            {"admin", "admin"},
            {"session", "session"},
            {"cookie", "cookie"},
            {"remember", "remember"},
            {"Secure", "Secure"},
            {"HttpOnly", "HttpOnly"},
            {"Expires", "Expires"}
        };

        var controllerDirectory = @"Areas\Admin\Controllers";
        var files = Directory.GetFiles(controllerDirectory, "*.cs");
        
        Console.WriteLine($"Found {files.Length} controller files to process...");
        
        foreach (var file in files)
        {
            try
            {
                Console.Write($"Processing {Path.GetFileName(file)}... ");
                
                // Read file content with UTF-8 encoding
                var content = File.ReadAllText(file, Encoding.UTF8);
                var originalLength = content.Length;
                
                // Apply all encoding fixes
                int fixCount = 0;
                foreach (var fix in encodingFixes)
                {
                    var oldContent = content;
                    content = content.Replace(fix.Key, fix.Value);
                    if (content != oldContent)
                        fixCount++;
                }
                
                // Write back with UTF-8 encoding
                File.WriteAllText(file, content, new UTF8Encoding(false));
                
                Console.WriteLine($"✓ Fixed {fixCount} patterns");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error: {ex.Message}");
            }
        }
        
        Console.WriteLine("\nAll Controllers encoding fixed successfully!");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
} 