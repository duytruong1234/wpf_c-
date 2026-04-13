# 🍕 Hệ Thống Quản Lý Kho Nguyên Liệu & Bán Hàng Pizza (PizzaInn Warehouse)

Một ứng dụng Desktop mạnh mẽ, giao diện cực kỳ hiện đại (Modern Fluent UI WPF), giúp tự động hoá quy trình quản lý kho nguyên liệu, bán hàng và nhân sự của nhà hàng Pizza.

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](#) 
[![WPF](https://img.shields.io/badge/WPF-Windows_Presentation_Foundation-blue)](#)
[![SQLite](https://img.shields.io/badge/Database-SQLite-003B57?logo=sqlite&logoColor=white)](#)

---

## 🚀 Tính năng nổi bật

Phần mềm được xây dựng theo chuẩn Kiến trúc thiết kế **MVVM (Model-View-ViewModel)** cùng mô hình **Dependency Injection** (phục vụ khả năng mở rộng), áp dụng ngôn ngữ thiết kế Fluent UI cho trải nghiệm người dùng tuyệt vời. Bao gồm các phân hệ sau:

* **📊 Bảng Điều Khiển (Dashboard):** Tổng quan doanh thu theo tháng/năm, hiển thị biểu đồ và thống kê lượng vật tư tồn kho, đơn đặt hàng, bộ lọc thời gian tiện ích.
* **📦 Quản Lý Kho Nguyên Liệu:** Theo dõi thông tin các loại nguyên liệu (bột, đường, phô mai...), quy đổi đơn vị, thông báo mức cảnh báo tồn kho.
* **🧾 Giao Dịch Nhập / Xuất:** Theo dõi luân chuyển hàng hoá. Khả năng in hoá đơn (`.PDF`, giấy máy in) cho từng Phiếu Nhập và Phiếu Xuất.
* **🛒 Bán Hàng & Đơn Hàng:** Giao diện POS bán Pizza. Theo dõi chi tiết hóa đơn khách hàng, lịch sử bán hàng và hoàn tất các thủ tục tài chính.
* **👥 Quản Lý Vận Hành:** 
  * Quản lý nhân viên (tài khoản, chức vụ, tình trạng...).
  * Quản lý nhà cung cấp.
  * Phân quyền bảo mật hệ thống.
* **⚙️ Cấu Hình & Hệ Thống:**
  * Lấy lại mật khẩu tự động qua tự động gửi mã an toàn OTP Email.
  * Cho phép chỉnh sửa quy định nhà hàng, thông tin cá nhân.
  * Tính năng Sao Lưu / Phục Hồi An toàn dữ liệu thông qua định dạng `.bak`.

## 🛠 Công nghệ áp dụng
- **Nền tảng:** C# / .NET 10 (WPF / Windows Presentation Foundation)
- **Giao diện:** [WPF UI (Fluent) by lepo.co](https://wpfui.lepo.co/) (chế độ Light theme đi kèm các thiết kế Custom Linear Gradients do Antigravity phác thảo)
- **Kiến trúc:** MVVM (với DataContext bindings) + Dependency Injection Container (Microsoft.Extensions.DependencyInjection)
- **Cơ sở dữ liệu:** Hệ CSDL thu gọn tối ưu hoá hiệu năng SQLite (`pizza_inventory.db` với công nghệ ADO.NET)

## 💻 Cách biên dịch & Cài đặt

### Dành cho Lập trình viên
1. Clone dự án về máy: `git clone https://github.com/duytruong1234/wpf_c-.git`
2. Đảm bảo bạn đã cài đặt SDK .NET 10 bản mới nhất.
3. Mở Terminal / PowerShell tới thư mục `QuanLyKhoNguyenLieuPizza` và chạy các lệnh:
   ```bash
   dotnet restore
   dotnet build
   dotnet run
   ```

### Đóng gói Ứng dụng & Cài đặt phần mềm (Inno Setup)
- Ứng dụng hiện hỗ trợ đóng gói tạo thành tệp thiết lập cài đặt `setup.exe` siêu gọn nhẹ.
- Tệp tin cấu hình bộ cài đã được cấu hình sẵn tại gốc dự án mang tên: `installer.iss`. Bạn chỉ cần cài đặt Compiler [Inno Setup](https://jrsoftware.org/isinfo.php), bật file `installer.iss` này lên chạy Run để xuất ra được tệp cài `PizzaInventory_Setup.exe` chuyên nghiệp và sẵn sàng phân phối thẳng tới máy điểm bán hàng!

---

*Cán đích với sự chuyên nghiệp, được thiết kế cho sự mở rộng vô hạn của nhà hàng Pizza!*
