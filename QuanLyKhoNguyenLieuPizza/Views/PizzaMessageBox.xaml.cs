using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace QuanLyKhoNguyenLieuPizza.Views
{
    public enum PizzaMessageBoxButtons { Ok, YesNo }
    public enum PizzaMessageBoxImage { Info, Warning, Error, Question, Success }

    public partial class PizzaMessageBox : Window
    {
        public bool Result { get; private set; }

        public PizzaMessageBox(string message, string title, PizzaMessageBoxButtons buttons = PizzaMessageBoxButtons.Ok, PizzaMessageBoxImage image = PizzaMessageBoxImage.Info)
        {
            InitializeComponent();
            TitleText.Text = title;
            MessageText.Text = message;

            // Handle Buttons
            if (buttons == PizzaMessageBoxButtons.Ok)
            {
                BtnCancel.Visibility = Visibility.Collapsed;
                BtnOk.Content = "OK";
            }
            else if (buttons == PizzaMessageBoxButtons.YesNo)
            {
                BtnCancel.Visibility = Visibility.Visible;
                BtnOk.Content = "Có";
                BtnCancel.Content = "Không";
            }

            // Handle Image
            switch (image)
            {
                case PizzaMessageBoxImage.Info:
                    IconSymbol.Symbol = Wpf.Ui.Controls.SymbolRegular.Info24;
                    IconSymbol.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6"));
                    BtnOk.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")); // Blue
                    break;
                case PizzaMessageBoxImage.Success:
                    IconSymbol.Symbol = Wpf.Ui.Controls.SymbolRegular.CheckmarkCircle24;
                    IconSymbol.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
                    BtnOk.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")); // Green
                    break;
                case PizzaMessageBoxImage.Warning:
                    IconSymbol.Symbol = Wpf.Ui.Controls.SymbolRegular.Warning24;
                    IconSymbol.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                    BtnOk.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")); // Yellow/Orange
                    break;
                case PizzaMessageBoxImage.Error:
                    IconSymbol.Symbol = Wpf.Ui.Controls.SymbolRegular.ErrorCircle24;
                    IconSymbol.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                    BtnOk.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")); // Red
                    break;
                case PizzaMessageBoxImage.Question:
                    IconSymbol.Symbol = Wpf.Ui.Controls.SymbolRegular.QuestionCircle24;
                    IconSymbol.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6"));
                    BtnOk.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")); // Blue
                    break;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Result = false;
            DialogResult = false;
            Close();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            Result = true;
            DialogResult = true;
            Close();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }

        public static bool Show(string message, string title, PizzaMessageBoxButtons buttons = PizzaMessageBoxButtons.Ok, PizzaMessageBoxImage image = PizzaMessageBoxImage.Info)
        {
            var dialog = new PizzaMessageBox(message, title, buttons, image);
            if (Application.Current.MainWindow != null && Application.Current.MainWindow.IsVisible)
            {
                dialog.Owner = Application.Current.MainWindow;
            }
            dialog.ShowDialog();
            return dialog.Result;
        }
    }
}
