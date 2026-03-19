using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QuanLyKhoNguyenLieuPizza.ViewModels;

namespace QuanLyKhoNguyenLieuPizza.Views;

public partial class OTPView : UserControl
{
    private OTPViewModel? ViewModel => DataContext as OTPViewModel;
    private bool _isSetup;
    
    // Cờ toàn cục để ngăn chặn sự kiện gọi lại lồng nhau
    private bool _isProcessing;
    
    // Lưu cache các ô OTP để truy cập theo chỉ mục dễ dàng
    private TextBox[] _otpBoxes = null!;

    public OTPView()
    {
        try
        {
            App.LogToFile("OTPView: Constructor START");
            InitializeComponent();
            App.LogToFile("OTPView: InitializeComponent OK");
            Loaded += OTPView_Loaded;
            App.LogToFile("OTPView: Constructor DONE");
        }
        catch (Exception ex)
        {
            App.LogToFile($"OTPView: Constructor ERROR: {ex}");
            throw;
        }
    }

    private void OTPView_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            App.LogToFile("OTPView: Loaded START");

            // Khởi tạo mảng các ô OTP
            _otpBoxes = new TextBox[] { otp1, otp2, otp3, otp4, otp5, otp6 };

            // Chỉ thiết lập sự kiện một lần
            if (!_isSetup)
            {
                foreach (var box in _otpBoxes)
                {
                    // Tắt IME (bàn phím tiếng Việt, v.v.) — OTP chỉ cần chữ số
                    InputMethod.SetIsInputMethodEnabled(box, false);
                    
                    box.PreviewKeyDown += OTPBox_PreviewKeyDown;
                    box.PreviewTextInput += OTPBox_PreviewTextInput;
                    box.TextChanged += OTPBox_TextChanged;
                    box.GotFocus += (s, args) =>
                    {
                        if (!_isProcessing)
                            ((TextBox)s).SelectAll();
                    };
                    DataObject.AddPastingHandler(box, OnPaste);
                }
                _isSetup = true;
            }

            // Xóa tất cả giá trị OTP một cách an toàn
            ClearAllBoxes();

            // Đặt con trỏ vào ô đầu tiên
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try { otp1.Focus(); } catch { }
            }), System.Windows.Threading.DispatcherPriority.Input);
            
            App.LogToFile("OTPView: Loaded DONE");
        }
        catch (Exception ex)
        {
            App.LogToFile($"OTPView: Loaded ERROR: {ex}");
            System.Diagnostics.Debug.WriteLine($"OTPView_Loaded ERROR: {ex}");
        }
    }

    /// <summary>
    /// Xóa tất cả các ô OTP một cách an toàn
    /// </summary>
    private void ClearAllBoxes()
    {
        if (_isProcessing) return;
        _isProcessing = true;
        try
        {
            foreach (var box in _otpBoxes)
            {
                box.Text = string.Empty;
            }
            SyncOTPCode();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ClearAllBoxes ERROR: {ex}");
        }
        finally
        {
            _isProcessing = false;
        }
    }

    /// <summary>
    /// Lưới an toàn: đảm bảo tối đa 1 chữ số mỗi ô. Xử lý mọi đầu vào bỏ qua PreviewTextInput.
    /// </summary>
    private void OTPBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // LUÔN kiểm tra _isProcessing để ngăn gọi lại lồng nhau
        if (_isProcessing) return;
        _isProcessing = true;
        
        try
        {
            var current = (TextBox)sender;
            var text = current.Text ?? string.Empty;
            
            // Loại bỏ các ký tự không phải chữ số (chữ cái từ IME, v.v.)
            var digitsOnly = new string(text.Where(char.IsDigit).ToArray());
            
            if (digitsOnly.Length == 0)
            {
                // Không có chữ số — xóa ô
                if (text.Length > 0)
                {
                    current.Text = string.Empty;
                }
                SyncOTPCode();
                return;
            }
            
            if (digitsOnly.Length == 1 && text == digitsOnly)
            {
                // Đúng 1 chữ số, nội dung sạch — chỉ đồng bộ
                SyncOTPCode();
                return;
            }
            
            // Có ký tự không phải số hoặc nhiều hơn 1 chữ số — sửa lại
            var index = GetBoxIndex(current);
            
            // Giữ chữ số cuối cùng (chữ số mới nhất được nhập)
            var keepDigit = digitsOnly[digitsOnly.Length - 1].ToString();
            current.Text = keepDigit;
            current.CaretIndex = 1;
            
            // Tự động chuyển sang ô tiếp theo
            if (index >= 0 && index < 5)
            {
                _otpBoxes[index + 1].Focus();
            }
            
            SyncOTPCode();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OTPBox_TextChanged ERROR: {ex}");
        }
        finally
        {
            _isProcessing = false;
        }
    }

    /// <summary>
    /// Trình xử lý đầu vào chính: chỉ cho phép nhập chữ số (0-9), tự động chuyển ô.
    /// </summary>
    private void OTPBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // Luôn xử lý — chúng ta quản lý toàn bộ văn bản
        e.Handled = true;

        if (_isProcessing) return;
        if (string.IsNullOrEmpty(e.Text) || !char.IsDigit(e.Text[0])) return;

        _isProcessing = true;
        try
        {
            var current = (TextBox)sender;
            var index = GetBoxIndex(current);
            if (index < 0) return;

            // Đặt chữ số
            current.Text = e.Text;
            current.CaretIndex = 1;

            // Tự động chuyển sang ô tiếp theo
            if (index < 5)
            {
                _otpBoxes[index + 1].Focus();
            }

            SyncOTPCode();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PreviewTextInput ERROR: {ex}");
        }
        finally
        {
            _isProcessing = false;
        }
    }

    /// <summary>
    /// Xử lý phím Backspace, Delete và điều hướng bằng phím mũi tên
    /// </summary>
    private void OTPBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_isProcessing) return;

        var current = (TextBox)sender;
        var index = GetBoxIndex(current);
        if (index < 0) return;

        try
        {
            switch (e.Key)
            {
                case Key.Back:
                    e.Handled = true;
                    _isProcessing = true;
                    try
                    {
                        if (!string.IsNullOrEmpty(current.Text))
                        {
                            current.Text = string.Empty;
                        }
                        else if (index > 0)
                        {
                            var prev = _otpBoxes[index - 1];
                            prev.Text = string.Empty;
                            prev.Focus();
                        }
                        SyncOTPCode();
                    }
                    finally
                    {
                        _isProcessing = false;
                    }
                    break;

                case Key.Delete:
                    e.Handled = true;
                    _isProcessing = true;
                    try
                    {
                        current.Text = string.Empty;
                        SyncOTPCode();
                    }
                    finally
                    {
                        _isProcessing = false;
                    }
                    break;

                case Key.Left:
                    if (index > 0)
                    {
                        e.Handled = true;
                        _otpBoxes[index - 1].Focus();
                    }
                    break;

                case Key.Right:
                    if (index < 5)
                    {
                        e.Handled = true;
                        _otpBoxes[index + 1].Focus();
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            _isProcessing = false;
            System.Diagnostics.Debug.WriteLine($"PreviewKeyDown ERROR: {ex}");
        }
    }

    /// <summary>
    /// Xử lý dán — điền tất cả 6 ô với các chữ số được dán
    /// </summary>
    private void OnPaste(object sender, DataObjectPastingEventArgs e)
    {
        try
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var text = (string)e.DataObject.GetData(typeof(string));
                var digits = new string(text.Where(char.IsDigit).ToArray());

                if (digits.Length >= 6)
                {
                    _isProcessing = true;
                    try
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            _otpBoxes[i].Text = digits[i].ToString();
                        }
                        otp6.Focus();
                        SyncOTPCode();
                    }
                    finally
                    {
                        _isProcessing = false;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _isProcessing = false;
            System.Diagnostics.Debug.WriteLine($"OnPaste ERROR: {ex}");
        }
        e.CancelCommand();
    }

    /// <summary>
    /// Lấy chỉ mục của TextBox trong mảng OTP (0-5), hoặc -1 nếu không tìm thấy
    /// </summary>
    private int GetBoxIndex(TextBox box)
    {
        if (_otpBoxes == null) return -1;
        for (int i = 0; i < _otpBoxes.Length; i++)
        {
            if (ReferenceEquals(_otpBoxes[i], box))
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Đồng bộ mã OTP đã nối vào ViewModel
    /// </summary>
    private void SyncOTPCode()
    {
        try
        {
            var vm = ViewModel;
            if (vm != null && _otpBoxes != null)
            {
                var code = string.Concat(_otpBoxes.Select(b => b.Text ?? string.Empty));
                vm.OTPCode = code;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SyncOTPCode ERROR: {ex}");
        }
    }
}
