using System.Windows;
using System.Windows.Controls;

namespace BrightstarDB.Polaris.Views
{
    public static class TextBoxHelper
    {
        #region IsSelectionMonitored

        public static readonly DependencyProperty IsSelectionMonitoredProperty =
            DependencyProperty.RegisterAttached(
                "IsSelectionMonitored", typeof (bool),
                typeof (TextBoxHelper),
                new FrameworkPropertyMetadata(OnIsSelectionMonitoredChanged));

        [AttachedPropertyBrowsableForType(typeof(TextBox))]
        public static bool GetIsSelectionMonitored(TextBox d)
        {
            return (bool) d.GetValue(IsSelectionMonitoredProperty);
        }

        public static void SetIsSelectionMonitored(TextBox d, bool value)
        {
            d.SetValue(IsSelectionMonitoredProperty, value);
        }

        private static void OnIsSelectionMonitoredChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e )
        {
            TextBox tb = obj as TextBox;
            if (tb!=null)
            {
                if ((bool)e.NewValue)
                {
                    tb.SelectionChanged += tb_SelectionChanged;
                }
                else
                {
                    tb.SelectionChanged -= tb_SelectionChanged;
                }
            }
        }
        #endregion

        public static string GetSelectedText(DependencyObject obj)
        {
            return (string)obj.GetValue(SelectedTextProperty);
        }

        public static void SetSelectedText(DependencyObject obj, string value)
        {
            obj.SetValue(SelectedTextProperty, value);
        }

        // Using a DependencyProperty as the backing store for SelectedText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedTextProperty =
            DependencyProperty.RegisterAttached(
                "SelectedText",
                typeof(string),
                typeof(TextBoxHelper),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SelectedTextChanged));

        private static void SelectedTextChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var tb = obj as TextBox;
            if (tb != null)
            {
                tb.SelectedText = (e.NewValue as string) ?? string.Empty;
            }

        }

        static void tb_SelectionChanged(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb != null)
            {
                SetSelectedText(tb, tb.SelectedText);
            }
        }

    }
}
