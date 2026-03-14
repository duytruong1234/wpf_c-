using QuanLyKhoNguyenLieuPizza.Models;

namespace QuanLyKhoNguyenLieuPizza.Services;

/// <summary>
/// Singleton to store current logged-in user session
/// </summary>
public class CurrentUserSession
{
    private static CurrentUserSession? _instance;
    private TaiKhoan? _currentUser;

    public static CurrentUserSession Instance => _instance ??= new CurrentUserSession();

    private CurrentUserSession() { }

    public TaiKhoan? CurrentUser => _currentUser;

    public string TenNguoiDung => _currentUser?.NhanVien?.HoTen ?? _currentUser?.Username ?? "Ng??i đãng";

    public bool IsLoggedIn => _currentUser != null;

    public void SetUser(TaiKhoan taiKhoan)
    {
        _currentUser = taiKhoan;
    }

    public void Logout()
    {
        _currentUser = null;
    }
}


