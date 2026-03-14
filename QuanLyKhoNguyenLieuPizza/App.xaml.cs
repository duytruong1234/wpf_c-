using System.IO;
using System.Windows;
using QuanLyKhoNguyenLieuPizza.Views;

namespace QuanLyKhoNguyenLieuPizza
{
    /// <summary>
    /// Interaction logic for App.xaml
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

            LogToFile("=== App Started ===");

            DispatcherUnhandledException += (s, args) =>
            {
                LogToFile($"DISPATCHER EXCEPTION: {args.Exception}");
                System.Diagnostics.Debug.WriteLine($"=== UNHANDLED EXCEPTION ===\n{args.Exception}");
                MessageBox.Show(
                    $"�� x?y ra l?i:\n{args.Exception.Message}\n\nChi ti?t:\n{args.Exception.StackTrace}",
                    "L?i ?ng d?ng",
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

            // Show splash screen
            var splash = new SplashWindow();
            splash.Show();

            // Wait for splash display (and any async initialization)
            await splash.WaitAndCloseAsync();
            splash.Close();

            // Open main window
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            var mainWindow = new MainWindow();
            try
            {
                mainWindow.Icon = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri("pack://application:,,,/Resources/Images/app.ico", UriKind.Absolute));
            }
            catch
            {
                // Icon resource not found � continue with default icon
            }
            MainWindow = mainWindow;
            mainWindow.Show();
        }
    }

}
