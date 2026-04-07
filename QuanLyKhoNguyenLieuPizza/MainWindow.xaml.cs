using System.Windows;
using System.Windows.Input;
using System.Windows.Shapes;

namespace QuanLyKhoNguyenLieuPizza
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            StateChanged += MainWindow_StateChanged;
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Tự động điều chỉnh cửa sổ theo màn hình, nhưng giới hạn kích thước tối đa hợp lý
            var workArea = SystemParameters.WorkArea;
            
            // Trên màn hình nhỏ (laptop), sử dụng 90% màn hình
            // Trên màn hình lớn (27"), giới hạn tối đa để tránh cửa sổ quá rộng
            double maxWidth = 1450;
            double maxHeight = 900;

            double targetWidth = Math.Min(workArea.Width * 0.92, maxWidth);
            double targetHeight = Math.Min(workArea.Height * 0.92, maxHeight);

            Width = Math.Max(MinWidth, targetWidth);
            Height = Math.Max(MinHeight, targetHeight);

            // Căn giữa màn hình
            Left = (workArea.Width - Width) / 2 + workArea.Left;
            Top = (workArea.Height - Height) / 2 + workArea.Top;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleMaximize();
            }
            else
            {
                DragMove();
            }
        }

        private void TitleBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Để trống có chủ đích - ngăn lỗi kéo thả
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleMaximize();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void ToggleMaximize()
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowState = WindowState.Maximized;
            }
        }

        private void MainWindow_StateChanged(object? sender, System.EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowBorder.CornerRadius = new CornerRadius(0);
                WindowBorder.BorderThickness = new Thickness(0);
                // Xử lý thanh tác vụ
                BorderThickness = new Thickness(7);
                MaximizeIcon.Data = System.Windows.Media.Geometry.Parse(
                    "M4,8 L4,4 L16,4 L16,16 L12,16 M2,8 L12,8 L12,18 L2,18 Z");
            }
            else
            {
                WindowBorder.CornerRadius = new CornerRadius(20);
                WindowBorder.BorderThickness = new Thickness(1);
                BorderThickness = new Thickness(0);
                MaximizeIcon.Data = System.Windows.Media.Geometry.Parse(
                    "M4,4 L20,4 L20,20 L4,20 Z");
            }
        }
    }
}
