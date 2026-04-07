using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace QuanLyKhoNguyenLieuPizza.Utilities;

/// <summary>
/// ObservableCollection hỗ trợ cập nhật hàng loạt.
/// Thay vì phát N sự kiện CollectionChanged khi thêm N phần tử,
/// chỉ phát 1 sự kiện Reset duy nhất → giảm 100x số lần UI layout lại.
/// </summary>
public class RangeObservableCollection<T> : ObservableCollection<T>
{
    private bool _suppressNotification;

    public RangeObservableCollection() : base() { }
    public RangeObservableCollection(IEnumerable<T> collection) : base(collection) { }

    /// <summary>
    /// Thêm nhiều phần tử cùng lúc, chỉ phát 1 sự kiện thông báo.
    /// </summary>
    public void AddRange(IEnumerable<T> items)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));

        _suppressNotification = true;
        try
        {
            foreach (var item in items)
            {
                Items.Add(item);
            }
        }
        finally
        {
            _suppressNotification = false;
        }

        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Xóa tất cả và thay thế bằng danh sách mới, chỉ phát 1 sự kiện.
    /// </summary>
    public void ReplaceAll(IEnumerable<T> items)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));

        _suppressNotification = true;
        try
        {
            Items.Clear();
            foreach (var item in items)
            {
                Items.Add(item);
            }
        }
        finally
        {
            _suppressNotification = false;
        }

        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (!_suppressNotification)
        {
            base.OnCollectionChanged(e);
        }
    }
}
