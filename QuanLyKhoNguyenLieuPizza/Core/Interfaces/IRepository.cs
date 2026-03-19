namespace QuanLyKhoNguyenLieuPizza.Core.Interfaces;

/// <summary>
/// Interface repository generic cho các thao tác CRUD cơ bản
/// </summary>
public interface IRepository<T> where T : class
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<T?> GetByIdAsync(int id);
    Task<bool> AddAsync(T entity);
    Task<bool> UpdateAsync(T entity);
    Task<bool> DeleteAsync(int id);
}

/// <summary>
/// Mẫu thiết kế Unit of Work cho quản lý giao dịch
/// </summary>
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}

