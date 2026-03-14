using System.Diagnostics;
using System.Windows;

namespace QuanLyKhoNguyenLieuPizza.Services;

public static class DiagnosticHelper
{
    public static void ShowError(string title, Exception ex)
    {
        Debug.WriteLine($"=== ERROR: {title} ===");
        Debug.WriteLine($"Message: {ex.Message}");
        Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
        
        if (ex.InnerException != null)
        {
            Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
        }
        
        MessageBox.Show(
            $"Error: {ex.Message}\n\nCheck Output window for details.",
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Error
        );
    }
    
    public static void ShowInfo(string message)
    {
        Debug.WriteLine($"INFO: {message}");
    }
}
