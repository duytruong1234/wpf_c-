using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace QuanLyKhoNguyenLieuPizza.Behaviors;

public static class NumericInputBehavior
{
    // Property cho số nguyên (chỉ 0-9)
    public static readonly DependencyProperty IsNumericOnlyProperty =
        DependencyProperty.RegisterAttached(
            "IsNumericOnly",
            typeof(bool),
            typeof(NumericInputBehavior),
            new PropertyMetadata(false, OnIsNumericOnlyChanged));

    public static bool GetIsNumericOnly(DependencyObject obj) => (bool)obj.GetValue(IsNumericOnlyProperty);
    public static void SetIsNumericOnly(DependencyObject obj, bool value) => obj.SetValue(IsNumericOnlyProperty, value);

    // Property cho số thực (cho phép 0-9 và dấu . hoặc ,)
    public static readonly DependencyProperty IsDecimalOnlyProperty =
        DependencyProperty.RegisterAttached(
            "IsDecimalOnly",
            typeof(bool),
            typeof(NumericInputBehavior),
            new PropertyMetadata(false, OnIsNumericOnlyChanged));

    public static bool GetIsDecimalOnly(DependencyObject obj) => (bool)obj.GetValue(IsDecimalOnlyProperty);
    public static void SetIsDecimalOnly(DependencyObject obj, bool value) => obj.SetValue(IsDecimalOnlyProperty, value);

    private static void OnIsNumericOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBox textBox)
        {
            bool isNumeric = GetIsNumericOnly(textBox);
            bool isDecimal = GetIsDecimalOnly(textBox);
            
            if (isNumeric || isDecimal)
            {
                textBox.PreviewTextInput -= OnPreviewTextInput;
                textBox.TextChanged -= OnTextChanged;
                DataObject.RemovePastingHandler(textBox, OnPasting);

                textBox.PreviewTextInput += OnPreviewTextInput;
                textBox.TextChanged += OnTextChanged;
                DataObject.AddPastingHandler(textBox, OnPasting);
                
                InputMethod.SetIsInputMethodEnabled(textBox, false); // Tắt bộ gõ (IME)
            }
            else
            {
                textBox.PreviewTextInput -= OnPreviewTextInput;
                textBox.TextChanged -= OnTextChanged;
                DataObject.RemovePastingHandler(textBox, OnPasting);
            }
        }
    }

    private static string GetRegexPattern(TextBox textBox)
    {
        bool isDecimal = GetIsDecimalOnly(textBox);
        return isDecimal ? @"^[0-9.,]+$" : @"^[0-9]+$";
    }

    private static string GetCleanRegexPattern(TextBox textBox)
    {
        bool isDecimal = GetIsDecimalOnly(textBox);
        return isDecimal ? @"[^0-9.,]" : @"[^0-9]";
    }

    private static void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            var text = textBox.Text;
            var cleanText = Regex.Replace(text, GetCleanRegexPattern(textBox), "");
            
            if (text != cleanText)
            {
                var caretIndex = textBox.CaretIndex;
                textBox.Text = cleanText;
                textBox.CaretIndex = Math.Min(caretIndex, cleanText.Length);
            }
        }
    }

    private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            e.Handled = !Regex.IsMatch(e.Text, GetRegexPattern(textBox));
        }
    }

    private static void OnPasting(object sender, DataObjectPastingEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var text = (string)e.DataObject.GetData(typeof(string))!;
                if (!Regex.IsMatch(text, GetRegexPattern(textBox)))
                    e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }
    }
}
