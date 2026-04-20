using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace QuanLyKhoNguyenLieuPizza.Views;

public partial class BanHangView : UserControl
{
    public BanHangView()
    {
        InitializeComponent();
    }

    private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !Regex.IsMatch(e.Text, @"^[0-9]+$");
    }

    private void NumericOnly_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(typeof(string)))
        {
            var text = (string)e.DataObject.GetData(typeof(string))!;
            if (!Regex.IsMatch(text, @"^[0-9]+$"))
                e.CancelCommand();
        }
        else
        {
            e.CancelCommand();
        }
    }
}
