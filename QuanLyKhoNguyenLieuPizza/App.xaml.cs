using System.IO;
using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using QuanLyKhoNguyenLieuPizza.Views;

namespace QuanLyKhoNguyenLieuPizza
{
    /// <summary>
    /// Logic tương tác cho App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly string CrashLogPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "crash.log");

        public static void LogToFile(string message)
        {
            try
            {
                File.AppendAllText(CrashLogPath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}");
            }
            catch { }
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Đặt định dạng ngày tháng theo Việt Nam (dd/MM/yyyy)
            var culture = new CultureInfo("vi-VN");
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(
                    XmlLanguage.GetLanguage(culture.IetfLanguageTag)));

            LogToFile("=== App Started ===");

            DispatcherUnhandledException += (s, args) =>
            {
                LogToFile($"DISPATCHER EXCEPTION: {args.Exception}");
                System.Diagnostics.Debug.WriteLine($"=== UNHANDLED EXCEPTION ===\n{args.Exception}");
                MessageBox.Show(
                    $"Đã xảy ra lỗi:\n{args.Exception.Message}\n\nChi tiết:\n{args.Exception.StackTrace}",
                    "Lỗi ứng dụng",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                args.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                LogToFile($"DOMAIN EXCEPTION: {ex}");
                System.Diagnostics.Debug.WriteLine($"=== DOMAIN UNHANDLED EXCEPTION ===\n{ex}");
            };

            TaskScheduler.UnobservedTaskException += (s, args) =>
            {
                LogToFile($"TASK EXCEPTION: {args.Exception}");
                System.Diagnostics.Debug.WriteLine($"=== UNOBSERVED TASK EXCEPTION ===\n{args.Exception}");
                args.SetObserved();
            };

            // Hiển thị màn hình khởi động
            var splash = new SplashWindow();
            splash.Show();

            // Đợi màn hình khởi động hiển thị (và khởi tạo bất đồng bộ)
            await splash.WaitAndCloseAsync();
            splash.Close();

            // Mở cửa sổ chính
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            var mainWindow = new MainWindow();
            try
            {
                mainWindow.Icon = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri("pack://application:,,,/Resources/Images/app.ico", UriKind.Absolute));
            }
            catch
            {
                // Không tìm thấy tài nguyên icon - tiếp tục với icon mặc định
            }
            MainWindow = mainWindow;
            mainWindow.Show();
        }
    }

}


