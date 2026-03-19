using System.Windows;
using System.Windows.Controls;

namespace QuanLyKhoNguyenLieuPizza.Views;

public partial class ChangePasswordView : UserControl
{
    public ChangePasswordView()
    {
        InitializeComponent();

        // Đăng ký sự kiện PasswordChanged để cập nhật hiển thị placeholder
        txtNewPassword.PasswordChanged += TxtNewPassword_PasswordChanged;
        txtConfirmPassword.PasswordChanged += TxtConfirmPassword_PasswordChanged;
    }

    private void TxtNewPassword_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox pb)
        {
            newPasswordPlaceholder.Visibility = string.IsNullOrEmpty(pb.Password)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }

    private void TxtConfirmPassword_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox pb)
        {
            confirmPasswordPlaceholder.Visibility = string.IsNullOrEmpty(pb.Password)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }
}

