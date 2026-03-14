using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QuanLyKhoNguyenLieuPizza.ViewModels;

namespace QuanLyKhoNguyenLieuPizza.Views;

public partial class OTPView : UserControl
{
    private OTPViewModel? ViewModel => DataContext as OTPViewModel;
    private bool _isSetup;

    public OTPView()
    {
        try
        {
            App.LogToFile("OTPView: Constructor START");
            InitializeComponent();
            App.LogToFile("OTPView: InitializeComponent OK");
            Loaded += OTPView_Loaded;
            App.LogToFile("OTPView: Constructor DONE");
        }
        catch (Exception ex)
        {
            App.LogToFile($"OTPView: Constructor ERROR: {ex}");
            throw;
        }
    }

    private void OTPView_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            App.LogToFile("OTPView: Loaded START");
            System.Diagnostics.Debug.WriteLine("=== OTPView_Loaded ===");

            // Chỉ setup event handlers một lần
            if (!_isSetup)
            {
                SetupOTPBox(otp1, null, otp2);
                SetupOTPBox(otp2, otp1, otp3);
                SetupOTPBox(otp3, otp2, otp4);
                SetupOTPBox(otp4, otp3, otp5);
                SetupOTPBox(otp5, otp4, otp6);
                SetupOTPBox(otp6, otp5, null);
                _isSetup = true;
                System.Diagnostics.Debug.WriteLine("=== All OTP boxes setup complete ===");
            }

            // Clear old OTP values
            otp1.Text = "";
            otp2.Text = "";
            otp3.Text = "";
            otp4.Text = "";
            otp5.Text = "";
            otp6.Text = "";

            // Focus first box
            Dispatcher.BeginInvoke(new Action(() => otp1.Focus()), System.Windows.Threading.DispatcherPriority.Input);
            App.LogToFile("OTPView: Loaded DONE");
            System.Diagnostics.Debug.WriteLine("=== Focused otp1 ===");
        }
        catch (Exception ex)
        {
            App.LogToFile($"OTPView: Loaded ERROR: {ex}");
            System.Diagnostics.Debug.WriteLine($"OTPView_Loaded ERROR: {ex}");
            MessageBox.Show($"OTPView_Loaded Error:\n{ex.Message}\n\n{ex.StackTrace}", "Debug Error");
        }
    }

    private void SetupOTPBox(TextBox current, TextBox? previous, TextBox? next)
    {
        // Handle text input
        current.TextChanged += (s, e) =>
        {
            try
            {
                if (current.Text.Length > 0)
                {
                    // Only keep first character
                    if (current.Text.Length > 1)
                    {
                        current.Text = current.Text[0].ToString();
                        current.CaretIndex = 1;
                        return; // TextChanged will fire again with single char
                    }

                    // Auto move to next box
                    if (next != null)
                    {
                        next.Focus();
                        next.SelectAll();
                    }

                    // Update ViewModel
                    UpdateOTPCode();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TextChanged ERROR: {ex}");
            }
        };

        // Handle keyboard navigation
        current.PreviewKeyDown += (s, e) =>
        {
            try
            {
                if (e.Key == Key.Back && string.IsNullOrEmpty(current.Text) && previous != null)
                {
                    previous.Focus();
                    previous.SelectAll();
                    e.Handled = true;
                }
                else if (e.Key == Key.Left && previous != null)
                {
                    previous.Focus();
                    previous.SelectAll();
                    e.Handled = true;
                }
                else if (e.Key == Key.Right && next != null)
                {
                    next.Focus();
                    next.SelectAll();
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PreviewKeyDown ERROR: {ex}");
            }
        };

        // Only allow numbers (0-9)
        current.PreviewTextInput += (s, e) =>
        {
            try
            {
                if (string.IsNullOrEmpty(e.Text) || !char.IsDigit(e.Text, 0))
                {
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PreviewTextInput ERROR: {ex}");
                e.Handled = true;
            }
        };

        // Handle focus - select all text
        current.GotFocus += (s, e) =>
        {
            current.SelectAll();
        };

        // Handle paste into any box
        DataObject.AddPastingHandler(current, OnPaste);
    }

    private void OnPaste(object sender, DataObjectPastingEventArgs e)
    {
        try
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var text = (string)e.DataObject.GetData(typeof(string));

                var digits = new string(text.Where(char.IsDigit).ToArray());

                if (digits.Length >= 6)
                {
                    otp1.Text = digits[0].ToString();
                    otp2.Text = digits[1].ToString();
                    otp3.Text = digits[2].ToString();
                    otp4.Text = digits[3].ToString();
                    otp5.Text = digits[4].ToString();
                    otp6.Text = digits[5].ToString();

                    otp6.Focus();
                    UpdateOTPCode();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnPaste ERROR: {ex}");
        }
        e.CancelCommand();
    }

    private void UpdateOTPCode()
    {
        try
        {
            if (ViewModel != null)
            {
                var code = $"{otp1.Text}{otp2.Text}{otp3.Text}{otp4.Text}{otp5.Text}{otp6.Text}";
                ViewModel.OTPCode = code;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateOTPCode ERROR: {ex}");
        }
    }
}


