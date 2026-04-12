using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QuanLyKhoNguyenLieuPizza.Models;
using QuanLyKhoNguyenLieuPizza.ViewModels;

namespace QuanLyKhoNguyenLieuPizza.Views
{
    public partial class QuyDinhView : UserControl
    {
        public QuyDinhView()
        {
            InitializeComponent();
        }

        private void EditBot_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement el && el.Tag is QuyDinh_Bot bot && DataContext is QuyDinhViewModel vm)
            {
                vm.EditBotCommand.Execute(bot);
            }
        }

        private void DeleteBot_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement el && el.Tag is QuyDinh_Bot bot && DataContext is QuyDinhViewModel vm)
            {
                vm.DeleteBotCommand.Execute(bot);
            }
        }

        private void EditVien_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement el && el.Tag is QuyDinh_Vien vien && DataContext is QuyDinhViewModel vm)
            {
                vm.EditVienCommand.Execute(vien);
            }
        }

        private void DeleteVien_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement el && el.Tag is QuyDinh_Vien vien && DataContext is QuyDinhViewModel vm)
            {
                vm.DeleteVienCommand.Execute(vien);
            }
        }
    }
}
