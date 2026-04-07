using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QuanLyKhoNguyenLieuPizza.ViewModels;

public abstract class BaseViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public virtual bool AnyDialogOpen => false;

    /// <summary>
    /// Cờ ngăn các setter property kích hoạt reload DB khi đang cập nhật hàng loạt.
    /// Ví dụ: ClearFilter() thay đổi 7 property → chỉ reload 1 lần cuối.
    /// </summary>
    protected bool IsBatchUpdating { get; set; }

    /// <summary>
    /// CancellationTokenSource dùng cho debounce tìm kiếm.
    /// </summary>
    private CancellationTokenSource? _debounceCts;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// Thực thi hành động bất đồng bộ với quản lý trạng thái bận tự động và xử lý lỗi.
    /// </summary>
    protected async Task SafeExecuteAsync(Func<Task> action, [CallerMemberName] string? caller = null)
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in {caller}: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Debounce: Chờ một khoảng thời gian sau lần gọi cuối trước khi thực thi.
    /// Dùng cho tìm kiếm → tránh gọi DB mỗi khi gõ 1 ký tự.
    /// Ví dụ: gõ "pizza" → chỉ tìm 1 lần thay vì 5 lần ("p","pi","piz","pizz","pizza").
    /// </summary>
    protected async Task DebounceAsync(Func<Task> action, int delayMs = 300)
    {
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        try
        {
            await Task.Delay(delayMs, token);
            if (!token.IsCancellationRequested)
            {
                await action();
            }
        }
        catch (TaskCanceledException)
        {
            // Bình thường - bị hủy do người dùng tiếp tục gõ
        }
    }

    /// <summary>
    /// Khởi tạo ViewModel an toàn thay vì fire-and-forget "_ = LoadDataAsync()".
    /// Bắt lỗi và log thay vì để exception chìm.
    /// </summary>
    protected async void SafeInitializeAsync(Func<Task> loadAction)
    {
        try
        {
            await loadAction();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing {GetType().Name}: {ex.Message}");
        }
    }
}
