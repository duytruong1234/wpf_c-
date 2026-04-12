using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace QuanLyKhoNguyenLieuPizza.Views
{
    public partial class SearchableComboBox : UserControl
    {
        #region Dependency Properties

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(SearchableComboBox),
                new PropertyMetadata(null, OnItemsSourceChanged));

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(SearchableComboBox),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));

        public static readonly DependencyProperty DisplayMemberPathProperty =
            DependencyProperty.Register(nameof(DisplayMemberPath), typeof(string), typeof(SearchableComboBox),
                new PropertyMetadata("name"));

        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(SearchableComboBox),
                new PropertyMetadata("-- Chọn --", OnPlaceholderChanged));

        public static readonly DependencyProperty SearchPlaceholderProperty =
            DependencyProperty.Register(nameof(SearchPlaceholder), typeof(string), typeof(SearchableComboBox),
                new PropertyMetadata("Tìm kiếm...", OnSearchPlaceholderChanged));

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public object? SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public string DisplayMemberPath
        {
            get => (string)GetValue(DisplayMemberPathProperty);
            set => SetValue(DisplayMemberPathProperty, value);
        }

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        public string SearchPlaceholder
        {
            get => (string)GetValue(SearchPlaceholderProperty);
            set => SetValue(SearchPlaceholderProperty, value);
        }

        #endregion

        private List<object> _allItems = new();
        private bool _isUpdatingSelection;

        public SearchableComboBox()
        {
            InitializeComponent();
            UpdateDisplayText();
            SearchPlaceholderText.Text = SearchPlaceholder;
        }

        #region Property Changed Callbacks

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchableComboBox control)
            {
                // Unsubscribe from old collection
                if (e.OldValue is INotifyCollectionChanged oldCollection)
                {
                    oldCollection.CollectionChanged -= control.OnCollectionChanged;
                }

                // Subscribe to new collection
                if (e.NewValue is INotifyCollectionChanged newCollection)
                {
                    newCollection.CollectionChanged += control.OnCollectionChanged;
                }

                control.RefreshItems();
            }
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Refresh items when the collection changes (items added/removed)
            Dispatcher.BeginInvoke(new Action(() => RefreshItems()));
        }

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchableComboBox control)
            {
                control.UpdateDisplayText();
                if (!control._isUpdatingSelection)
                {
                    control._isUpdatingSelection = true;
                    control.ItemsListBox.SelectedItem = e.NewValue;
                    control._isUpdatingSelection = false;
                }
            }
        }

        private static void OnPlaceholderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchableComboBox control)
            {
                control.UpdateDisplayText();
            }
        }

        private static void OnSearchPlaceholderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchableComboBox control)
            {
                control.SearchPlaceholderText.Text = e.NewValue as string ?? "Tìm kiếm...";
            }
        }

        #endregion

        #region Methods

        private void RefreshItems()
        {
            _allItems.Clear();
            if (ItemsSource != null)
            {
                foreach (var item in ItemsSource)
                {
                    _allItems.Add(item);
                }
            }

            // Only update the ListBox if the popup is NOT open to avoid "inconsistent" errors
            if (!DropdownPopup.IsOpen)
            {
                ApplyFilter(string.Empty);
            }
            UpdateDisplayText();
        }

        private void ApplyFilter(string searchText)
        {
            List<object> displayItems;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                displayItems = new List<object>(_allItems);
            }
            else
            {
                displayItems = _allItems.Where(item =>
                {
                    var displayValue = GetDisplayValue(item);
                    return displayValue != null &&
                           displayValue.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                }).ToList();
            }

            // Set a NEW list each time to avoid "inconsistent with its items source" error
            ItemsListBox.ItemsSource = displayItems;
            ItemsListBox.DisplayMemberPath = DisplayMemberPath;
            EmptyText.Visibility = displayItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            // Restore selection highlight
            if (SelectedItem != null)
            {
                _isUpdatingSelection = true;
                ItemsListBox.SelectedItem = SelectedItem;
                _isUpdatingSelection = false;
            }
        }

        private string? GetDisplayValue(object? item)
        {
            if (item == null) return null;
            if (string.IsNullOrEmpty(DisplayMemberPath)) return item.ToString();

            var prop = item.GetType().GetProperty(DisplayMemberPath);
            return prop?.GetValue(item)?.ToString();
        }

        private void UpdateDisplayText()
        {
            var displayTextBlock = FindDisplayTextBlock();
            if (displayTextBlock == null) return;

            if (SelectedItem != null)
            {
                var displayValue = GetDisplayValue(SelectedItem);
                displayTextBlock.Text = displayValue ?? Placeholder;
                displayTextBlock.Foreground = FindResource("SCB_Gray800") as Brush ?? Brushes.Black;
            }
            else
            {
                displayTextBlock.Text = Placeholder;
                displayTextBlock.Foreground = FindResource("SCB_Gray400") as Brush ?? Brushes.Gray;
            }
        }

        private TextBlock? FindDisplayTextBlock()
        {
            if (ToggleBtn?.Template == null) return null;
            return ToggleBtn.Template.FindName("PART_DisplayText", ToggleBtn) as TextBlock;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            ToggleBtn.Loaded += (s, e) => UpdateDisplayText();
        }

        #endregion

        #region Event Handlers

        private void DropdownPopup_Opened(object sender, EventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            ApplyFilter(string.Empty);

            SearchTextBox.Dispatcher.BeginInvoke(new Action(() =>
            {
                SearchTextBox.Focus();
                Keyboard.Focus(SearchTextBox);
            }), System.Windows.Threading.DispatcherPriority.Render);
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var text = SearchTextBox.Text;
            SearchPlaceholderText.Visibility = string.IsNullOrEmpty(text) ? Visibility.Visible : Visibility.Collapsed;
            ApplyFilter(text);
        }

        private void ItemsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingSelection) return;
            if (ItemsListBox.SelectedItem == null) return;

            _isUpdatingSelection = true;
            SelectedItem = ItemsListBox.SelectedItem;
            _isUpdatingSelection = false;

            DropdownPopup.IsOpen = false;
        }

        private void DropdownPopup_Closed(object sender, EventArgs e)
        {
            ToggleBtn.IsChecked = false;
        }

        #endregion
    }
}
