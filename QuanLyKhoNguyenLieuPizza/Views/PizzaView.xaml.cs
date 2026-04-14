using System.Windows.Controls;

namespace QuanLyKhoNguyenLieuPizza.Views;

public partial class PizzaView : UserControl
{
    public PizzaView()
    {
        InitializeComponent();
    }

    private void NumberOnly_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        // Cho phép nhập số cơ bản, dấu phẩy, và dấu chấm
        e.Handled = !System.Text.RegularExpressions.Regex.IsMatch(e.Text, @"^[0-9,.]+$");
    }
}

