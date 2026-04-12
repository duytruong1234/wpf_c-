using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace QuanLyKhoNguyenLieuPizza.Views;

public class DeleteConfirmWindow : Window
{
    public bool Result { get; private set; }

    public DeleteConfirmWindow(string title, string message)
    {
        Title = "";
        Width = SystemParameters.PrimaryScreenWidth;
        Height = SystemParameters.PrimaryScreenHeight;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0));
        ResizeMode = ResizeMode.NoResize;
        Topmost = true;
        ShowInTaskbar = false;

        // Click overlay → cancel
        MouseLeftButtonDown += (s, e) => { Result = false; DialogResult = false; };

        // Card
        var card = new Border
        {
            Width = 440,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Background = Brushes.White,
            CornerRadius = new CornerRadius(20),
            Effect = new DropShadowEffect
            {
                BlurRadius = 40, ShadowDepth = 8, Direction = 270,
                Color = Colors.Black, Opacity = 0.18
            }
        };
        // Stop click from propagating to overlay
        card.MouseLeftButtonDown += (s, e) => e.Handled = true;

        var stack = new StackPanel();

        // ── Icon ──
        var outerCircle = new Border
        {
            Width = 72, Height = 72, CornerRadius = new CornerRadius(36),
            Background = Brush("#FEE2E2"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 32, 0, 16)
        };
        var innerCircle = new Border
        {
            Width = 50, Height = 50, CornerRadius = new CornerRadius(25),
            Background = Brush("#FECACA"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        innerCircle.Child = new TextBlock
        {
            Text = "🗑️",
            FontSize = 24,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        outerCircle.Child = innerCircle;
        stack.Children.Add(outerCircle);

        // ── Title ──
        stack.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 20, FontWeight = FontWeights.Bold,
            Foreground = Brush("#1E293B"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(32, 0, 32, 12)
        });

        // ── Message ──
        stack.Children.Add(new TextBlock
        {
            Text = message,
            FontSize = 13, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center,
            Foreground = Brush("#64748B"),
            MaxWidth = 340, LineHeight = 20,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(32, 0, 32, 28)
        });

        // ── Buttons ──
        var btnGrid = new Grid { Margin = new Thickness(32, 0, 32, 28), Height = 44 };
        btnGrid.ColumnDefinitions.Add(new ColumnDefinition());
        btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
        btnGrid.ColumnDefinitions.Add(new ColumnDefinition());

        // Cancel
        var cancelBorder = new Border
        {
            Background = Brush("#F1F5F9"),
            CornerRadius = new CornerRadius(12),
            BorderBrush = Brush("#E2E8F0"),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand
        };
        cancelBorder.Child = new TextBlock
        {
            Text = "Hủy bỏ", FontSize = 14, FontWeight = FontWeights.SemiBold,
            Foreground = Brush("#64748B"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        cancelBorder.MouseEnter += (s, e) => cancelBorder.Background = Brush("#E2E8F0");
        cancelBorder.MouseLeave += (s, e) => cancelBorder.Background = Brush("#F1F5F9");
        cancelBorder.MouseLeftButtonDown += (s, e) => { e.Handled = true; Result = false; DialogResult = false; };
        Grid.SetColumn(cancelBorder, 0);
        btnGrid.Children.Add(cancelBorder);

        // Delete
        var deleteBorder = new Border
        {
            CornerRadius = new CornerRadius(12),
            Cursor = Cursors.Hand,
            Background = new LinearGradientBrush(
                (Color)ColorConverter.ConvertFromString("#EF4444")!,
                (Color)ColorConverter.ConvertFromString("#DC2626")!,
                0),
            Effect = new DropShadowEffect
            {
                BlurRadius = 12, ShadowDepth = 2, Direction = 270,
                Color = (Color)ColorConverter.ConvertFromString("#EF4444")!, Opacity = 0.3
            }
        };
        var deleteStack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        deleteStack.Children.Add(new TextBlock
        {
            Text = "🗑", FontSize = 13, Foreground = Brushes.White,
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center
        });
        deleteStack.Children.Add(new TextBlock
        {
            Text = "Xóa", FontSize = 14, FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });
        deleteBorder.Child = deleteStack;
        deleteBorder.MouseEnter += (s, e) => deleteBorder.Opacity = 0.9;
        deleteBorder.MouseLeave += (s, e) => deleteBorder.Opacity = 1.0;
        deleteBorder.MouseLeftButtonDown += (s, e) => { e.Handled = true; Result = true; DialogResult = true; };
        Grid.SetColumn(deleteBorder, 2);
        btnGrid.Children.Add(deleteBorder);

        stack.Children.Add(btnGrid);
        card.Child = stack;

        Content = card;
    }

    private static SolidColorBrush Brush(string hex)
        => new((Color)ColorConverter.ConvertFromString(hex)!);
}
