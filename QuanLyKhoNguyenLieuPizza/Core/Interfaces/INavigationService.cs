namespace QuanLyKhoNguyenLieuPizza.Core.Interfaces;

/// <summary>
/// Interface dịch vụ điều hướng để tách logic điều hướng khỏi ViewModels
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// View hiện tại đang hiển thị
    /// </summary>
    object? CurrentView { get; }
    
    /// <summary>
    /// Điều hướng đến một kiểu ViewModel cụ thể
    /// </summary>
    void NavigateTo<TViewModel>() where TViewModel : class;
    
    /// <summary>
    /// Điều hướng đến một ViewModel cụ thể với tham số
    /// </summary>
    void NavigateTo<TViewModel>(object parameter) where TViewModel : class;
    
    /// <summary>
    /// Điều hướng quảy lại view trước đó
    /// </summary>
    void GoBack();
    
    /// <summary>
    /// Kiểm tra có thể quay lại không
    /// </summary>
    bool CanGoBack { get; }
    
    /// <summary>
    /// Sự kiện khi điều hướng xảy ra
    /// </summary>
    event Action<object?>? OnNavigated;
}

