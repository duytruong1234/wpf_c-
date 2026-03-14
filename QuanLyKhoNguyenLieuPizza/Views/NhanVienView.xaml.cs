using System.Windows.Controls;
using QuanLyKhoNguyenLieuPizza.ViewModels;

namespace QuanLyKhoNguyenLieuPizza.Views;

public partial class NhanVienView : UserControl
{
    public NhanVienView()
    {
        InitializeComponent();
    }

    private void PasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is NhanVienViewModel vm && sender is PasswordBox pb)
        {
            vm.FormPassword = pb.Password;
        }
    }
}
