import os
import re

dict_map = {
    "Hệ Số Quy Đổi Button": "Nút Hệ Số Quy Đổi",
    "Card Style": "Kiểu thẻ",
    "Converters": "Bộ chuyển đổi",
    "Gradient for Popup Header": "Gradient cho Tiêu đề Popup",
    "Primary Button": "Nút Chính",
    "Icon Button Style": "Kiểu Nút Icon",
    "Filter Chip Button": "Nút Bộ lọc Thẻ",
    "Material Card with Hover": "Thẻ Material kèm Hover",
    "Main Content": "Nội dung Chính",
    "Title": "Tiêu đề",
    "Refresh Button": "Nút Làm mới",
    "Nhập Kho Button": "Nút Nhập Kho",
    "Xuất Kho Button": "Nút Xuất Kho",
    "Total Stat": "Thống kê Tổng",
    "Search Box": "Hộp Tìm kiếm",
    "Filter Chips": "Bộ lọc Thẻ",
    "Tất Cả": "Tất Cả",
    "Tồn Thấp": "Tồn Thấp",
    "Tồn Cao": "Tồn Cao",
    "Table Header": "Tiêu đề Bảng",
    "Table Rows": "Hàng Bảng",
    "Mã NL": "Mã NL",
    "Name with Image": "Tên kèm Hình ảnh",
    "Quantity": "Số lượng",
    "Unit": "Đơn vị",
    "Status Badge": "Thẻ Trạng thái",
    "Empty State": "Trạng thái Trống",
    "Backdrop": "Nền phủ",
    "Popup Card": "Thẻ Popup",
    "Popup Header": "Tiêu đề Popup",
    "Popup Content": "Nội dung Popup",
    "Left Panel": "Panel Trái",
    "Category Filter": "Bộ lọc Danh mục",
    "Material Grid": "Lưới Nguyên liệu",
    "Image": "Hình ảnh",
    "Fallback Icon": "Icon Thay thế",
    "Right Panel": "Panel Phải",
    "Selected Material": "Nguyên liệu đã chọn",
    "Conversion Table": "Bảng Quy đổi",
    "Editable HeSo": "Hệ số chỉnh sửa",
    "Nút Xóa": "Nút Xóa",
    "Action Buttons": "Nút Hành động",
    "PAGE HEADER": "TIÊU ĐỀ TRANG",
    "STAT CARDS": "THẺ THỐNG KÊ",
    "POS CONTENT: Menu + Cart": "NỘI DUNG POS: Thực đơn + Giỏ hàng",
    "PRODUCT MENU": "THỰC ĐƠN SẢN PHẨM",
    "Search": "Tìm kiếm",
    "Empty": "Trống",
    "Cart Header": "Tiêu đề Giỏ hàng",
    "Cart empty": "Giỏ hàng trống",
    "Header": "Tiêu đề",
    "Orders Table": "Bảng Đơn hàng",
    "Form": "Biểu mẫu",
    "De Banh": "Đế bánh",
    "Footer": "Chân trang",
    "Buttons": "Nút bấm",
    "Error Message": "Thông báo lỗi",
    "New Password": "Mật khẩu mới",
    "Placeholder": "Nơi giữ chỗ",
    "Hover Card Style": "Kiểu thẻ khi hover",
    "Custom WPF Canvas Chart": "Biểu đồ Canvas WPF tùy chỉnh",
    "Stock Status: Progress Bars": "Trạng thái kho: Thanh tiến trình",
    "Custom WPF Progress Bar Status": "Thanh trạng thái tiến trình WPF tùy chỉnh",
    "Table Body": "Nội dung bảng",
    "Refresh": "Làm mới",
    "ORDER TABLE": "BẢNG ĐƠN HÀNG",
    "Scrollable Content": "Nội dung cuộn",
    "CT Header": "Tiêu đề CT",
    "CT Body": "Nội dung CT",
    "Page Header": "Tiêu đề Trang",
    "Data Table": "Bảng Dữ liệu",
    "Form Content": "Nội dung Biểu mẫu",
    "Toggle Button Style": "Kiểu Nút Bật/Tắt",
    "Top Accent Bar": "Thanh trang trí trên",
    "Outer soft glow": "Hiệu ứng viền sáng mờ",
    "Cheese spots": "Đốm phô mai",
    "Remember Me & Forgot Password": "Ghi nhớ & Quên mật khẩu",
    "Button Styles": "Kiểu Nút",
    "Summary Stat Card": "Thẻ Thống kê",
    "Two column layout for dropdowns": "Bố cục 2 cột cho dropdown",
    "Toggle Button": "Nút Bật/Tắt",
    "MODERN COLOR PALETTE": "BẢNG MÀU HIỆN ĐẠI",
    "Table Header Style": "Kiểu Tiêu đề Bảng",
    "Table Cell Style": "Kiểu Ô Bảng",
    "Add Button": "Nút Thêm",
    "Total": "Tổng số",
    "Transparent ComboBox Style": "Kiểu ComboBox trong suốt",
    "Arrow": "Mũi tên",
    "Step Indicator": "Chỉ báo Bước",
    "Icon - Key/Verify": "Icon Xác minh",
    "Ho Ten": "Họ Tên",
    "Ngay Sinh": "Ngày Sinh",
    "Chuc Vu": "Chức Vụ",
    "Email": "Email",
    "Loading Overlay": "Màn hình Chờ"
}

import glob

files = glob.glob(r"d:\QuanLyKhoNguyenLieuPizza\QuanLyKhoNguyenLieuPizza\Views\*.xaml")

def replace_comment(match):
    inner = match.group(1).strip()
    
    # Clean up parens matching like "(no border, proper popup dropdown)" or "(Pill)"
    inner = re.sub(r"\s*\(.*?\)", "", inner)
    
    # Exact match first
    for k, v in dict_map.items():
        if inner.lower() == k.lower():
            return "<!-- " + v + " -->"
            
    # Partial match
    res_inner = inner
    for k, v in dict_map.items():
        if k.lower() in res_inner.lower() and v.lower() not in res_inner.lower():
            # Replace ignoring case
            res_inner = re.sub(re.escape(k), v, res_inner, flags=re.IGNORECASE)
            
    # Final cleanup for generic words if it's primarily english
    if re.fullmatch(r"^[A-Za-z\s\-\:]+$", res_inner):
        res_inner = res_inner.replace("Button", "Nút").replace("Title", "Tiêu đề").replace("Header", "Tiêu đề").replace("Footer", "Chân trang").replace("Style", "Kiểu").replace("Icon", "Biểu tượng").replace("Panel", "Bảng điều khiển").replace("Content", "Nội dung").replace("Box", "Hộp").replace("Text", "Chữ").replace("Image", "Ảnh").replace("Table", "Bảng").replace("Rows", "Hàng")
        
    return "<!-- " + res_inner + " -->"

changed_count = 0
for file in files:
    if "obj" in file or "bin" in file:
        continue
        
    with open(file, "r", encoding="utf-8") as f:
        content = f.read()
        
    new_content = re.sub(r"<!--\s*(.*?)\s*-->", replace_comment, content)
    
    if new_content != content:
        # Write back as utf-8, without bom
        with open(file, "w", encoding="utf-8") as f:
            f.write(new_content)
        changed_count += 1
        print("Updated:", os.path.basename(file))
        
print("Finished. Updated", changed_count, "files.")
