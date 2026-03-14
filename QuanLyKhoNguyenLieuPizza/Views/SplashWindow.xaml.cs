using System.Windows;
using System.Windows.Media.Animation;

namespace QuanLyKhoNguyenLieuPizza.Views
{
    public partial class SplashWindow : Window
    {
        public SplashWindow()
        {
            InitializeComponent();
        }

        public async Task WaitAndCloseAsync(int delayMilliseconds = 2500)
        {
            await Task.Delay(delayMilliseconds);

            var fadeOut = (Storyboard)FindResource("FadeOutStoryboard");
            var tcs = new TaskCompletionSource<bool>();
            fadeOut.Completed += (s, e) => tcs.SetResult(true);
            fadeOut.Begin(this);
            await tcs.Task;
        }
    }
}
