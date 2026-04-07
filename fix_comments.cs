using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { "PAGE HEADER", "TIÊU ĐỀ TRANG" },
            { "STAT CARDS", "THẺ THỐNG KÊ" },
            { "POS CONTENT: Menu + Cart", "NỘI DUNG POS: Thực đơn + Giỏ hàng" },
            { "PRODUCT MENU", "THỰC ĐƠN SẢN PHẨM" },
            { "Search", "Tìm kiếm" },
            { "Empty", "Trống" },
            { "Cart Header", "Tiêu đề giỏ hàng" },
            { "Table Header", "Tiêu đề bảng" },
            { "Cart empty", "Giỏ hàng trống" },
            { "Header", "Tiêu đề" },
            { "Orders Table", "Bảng đơn hàng" },
            { "Form", "Biểu mẫu" },
            { "De Banh", "Đế bánh" },
            { "Footer", "Chân trang" },
            { "Buttons", "Nút bấm" },
            { "Error Message", "Thông báo lỗi" },
            { "New Password", "Mật khẩu mới" },
            { "Placeholder", "Nơi giữ chỗ" },
            { "Card Style", "Kiểu thẻ" },
            { "Hover Card Style", "Kiểu thẻ khi hover" },
            { "Refresh Button", "Nút làm mới" },
            { "Custom WPF Canvas Chart", "Biểu đồ Canvas WPF tùy chỉnh" },
            { "Stock Status: Progress Bars", "Trạng thái kho: Thanh tiến trình" },
            { "Custom WPF Progress Bar Status", "Thanh trạng thái tiến trình WPF tùy chỉnh" },
            { "Table Body", "Nội dung bảng" },
            { "Backdrop", "Nền phủ" },
            { "Popup Card", "Thẻ Popup popup" },
            { "Popup Header", "Tiêu đề Popup" },
            { "Popup Content", "Nội dung Popup" },
            { "Empty State", "Trạng thái Trống" },
            { "Refresh", "Làm mới" },
            { "ORDER TABLE", "BẢNG ĐƠN HÀNG" },
            { "Scrollable Content", "Nội dung cuộn" },
            { "CT Header", "Tiêu đề CT" },
            { "CT Body", "Nội dung CT" },
            { "Page Header", "Tiêu đề Trang" },
            { "Data Table", "Bảng Dữ liệu" },
            { "Form Content", "Nội dung Form" },
            { "Toggle Button Style", "Kiểu Toggle Button" },
            { "Top Accent Bar", "Thanh trang trí trên" },
            { "Outer soft glow", "Hiệu ứng viền sáng mờ" },
            { "Cheese spots", "Đốm phô mai" },
            { "Remember Me & Forgot Password", "Ghi nhớ & Quên mật khẩu" },
            { "Button Styles", "Kiểu Nút" },
            { "Search Box", "Hộp Tìm kiếm" },
            { "Summary Stat Card", "Thẻ thống kê tổng quát" },
            { "Two column layout for dropdowns", "Bố cục 2 cột cho dropdown" },
            { "Toggle Button", "Nút Bật/Tắt" },
            { "MODERN COLOR PALETTE", "BẢNG MÀU HIỆN ĐẠI" },
            { "Table Header Style", "Kiểu Tiêu đề Bảng" },
            { "Table Cell Style", "Kiểu Ô Bảng" },
            { "Add Button", "Nút Thêm" },
            { "Total", "Tổng số" },
            { "Transparent ComboBox Style", "Kiểu ComboBox trong suốt" },
            { "Arrow", "Mũi tên" },
            { "Step Indicator", "Chỉ báo Bước" },
            { "Icon - Key/Verify", "Icon - Khóa/Xác minh" },
            { "Ho Ten", "Họ Tên" },
            { "Ngay Sinh", "Ngày Sinh" },
            { "Chuc Vu", "Chức Vụ" },
            { "Email", "Email" },
            { "Loading Overlay", "Lớp phủ Tải" },
            { "Converters", "Bộ chuyển đổi (Converters)" },
            { "Gradient for Popup Header", "Gradient cho Tiêu đề Popup" },
            { "Primary Button", "Nút Chính" },
            { "Icon Button Style", "Kiểu Nút Icon" },
            { "Filter Chip Button", "Nút Filter Chip" },
            { "Material Card with Hover", "Thẻ Material kèm Hover" },
            { "Main Content", "Nội dung Chính" },
            { "Title", "Tiêu đề" },
            { "Nhập Kho Button", "Nút Nhập Kho" },
            { "Xuất Kho Button", "Nút Xuất Kho" },
            { "Total Stat", "Thống kê Tổng" },
            { "Filter Chips", "Hiển thị Filter Chips" },
            { "Table Rows", "Các Hàng Bảng" },
            { "Name with Image", "Tên kèm Hình ảnh" },
            { "Quantity", "Số lượng" },
            { "Unit", "Đơn vị" },
            { "Status Badge", "Badge Trạng thái" },
            { "Left Panel", "Panel Trái" },
            { "Category Filter", "Bộ lọc Danh mục" },
            { "Material Grid", "Lưới Nguyên Liệu" },
            { "Image", "Hình ảnh" },
            { "Fallback Icon", "Icon Thay thế" },
            { "Right Panel", "Panel Phải" },
            { "Selected Material", "Nguyên liệu đã chọn" },
            { "Conversion Table", "Bảng Quy đổi" },
            { "Editable HeSo", "Hệ số có thể chỉnh sửa" },
            { "Secondary Output", "Đầu ra thứ cấp" },
            { "Confirm Buttons", "Nút Xác nhận" }
        };

        var files = Directory.GetFiles(@\"d:\QuanLyKhoNguyenLieuPizza\QuanLyKhoNguyenLieuPizza\", \"*.xaml\", SearchOption.AllDirectories);
        int changedFiles = 0;
        foreach (var file in files)
        {
            if (file.Contains(\"\\obj\\\") || file.Contains(\"\\bin\\\")) continue;
            
            string content = File.ReadAllText(file, Encoding.UTF8);
            bool changed = false;

            string newContent = Regex.Replace(content, @\"<!--\s*(.*?)\s*-->\", match => {
                string inner = match.Groups[1].Value.Trim();
                // simple direct match
                if (dict.TryGetValue(inner, out string trans)) {
                    changed = true;
                    return \"<!-- \" + trans + \" -->\";
                }
                
                // partial translation for \"Transparent ComboBox Style...\" etc
                foreach(var kv in dict) {
                    if(inner.Contains(kv.Key) && !inner.Contains(kv.Value)) {
                        inner = inner.Replace(kv.Key, kv.Value);
                        changed = true;
                    }
                }
                return \"<!-- \" + inner + \" -->\";
            });

            if (changed)
            {
                File.WriteAllText(file, newContent, new UTF8Encoding(true));
                changedFiles++;
                Console.WriteLine(\"Updated: \" + Path.GetFileName(file));
            }
        }
        Console.WriteLine(\"Finished. Updated \" + changedFiles + \" files.\");
    }
}
