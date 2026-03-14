namespace QuanLyKhoNguyenLieuPizza.Core.Interfaces;

/// <summary>
/// Navigation service interface for decoupling navigation logic from ViewModels
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Current view being displayed
    /// </summary>
    object? CurrentView { get; }
    
    /// <summary>
    /// Navigate to a specific ViewModel type
    /// </summary>
    void NavigateTo<TViewModel>() where TViewModel : class;
    
    /// <summary>
    /// Navigate to a specific ViewModel with parameter
    /// </summary>
    void NavigateTo<TViewModel>(object parameter) where TViewModel : class;
    
    /// <summary>
    /// Navigate back to previous view
    /// </summary>
    void GoBack();
    
    /// <summary>
    /// Check if can go back
    /// </summary>
    bool CanGoBack { get; }
    
    /// <summary>
    /// Event fired when navigation occurs
    /// </summary>
    event Action<object?>? OnNavigated;
}

