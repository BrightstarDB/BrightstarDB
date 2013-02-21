using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace BrightstarDB.Polaris.Controls
{
    [TemplatePart(Name = "PART_TabHeader", Type = typeof (TextBox))]
    public class EditableTabHeaderControl : ContentControl
    {
        /// <summary>
        /// Dependency property to bind EditMode with XAML Trigger
        /// </summary>
        private static readonly DependencyProperty IsInEditModeProperty =
            DependencyProperty.Register("IsInEditMode", typeof (bool), typeof (EditableTabHeaderControl));

        private string _oldText;
        private TextBox _textBox;
        private DispatcherTimer _timer;

        /// <summary>
        /// Gets or sets a value indicating whether this instance is in edit mode.
        /// </summary>
        public bool IsInEditMode
        {
            get { return (bool) GetValue(IsInEditModeProperty); }
            set
            {
                if (string.IsNullOrEmpty(_textBox.Text))
                {
                    _textBox.Text = _oldText;
                }

                _oldText = _textBox.Text;
                SetValue(IsInEditModeProperty, value);
            }
        }

        /// <summary>
        /// When overridden in a derived class, is invoked whenever 
        /// application code or internal processes call 
        /// <see cref="M:System.Windows.FrameworkElement.ApplyTemplate"/>.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _textBox = Template.FindName("PART_TabHeader", this) as TextBox;
            if (_textBox != null)
            {
                _timer = new DispatcherTimer();
                _timer.Tick += TimerTick;
                _timer.Interval = TimeSpan.FromMilliseconds(1);
                LostFocus += TextBoxLostFocus;
                _textBox.KeyDown += TextBoxKeyDown;
                MouseDoubleClick += EditableTabHeaderControlMouseDoubleClick;
            }
        }

        /// <summary>
        /// Sets the IsInEdit mode.
        /// </summary>
        /// <param name="value">if set to <c>true</c> [value].</param>
        public void SetEditMode(bool value)
        {
            IsInEditMode = value;
            _timer.Start();
        }

        private void TimerTick(object sender, EventArgs e)
        {
            _timer.Stop();
            MoveTextBoxInFocus();
        }

        private void MoveTextBoxInFocus()
        {
            if (_textBox.CheckAccess())
            {
                if (!string.IsNullOrEmpty(_textBox.Text))
                {
                    _textBox.CaretIndex = 0;
                    _textBox.Focus();
                }
            }
            else
            {
                _textBox.Dispatcher.BeginInvoke
                    (DispatcherPriority.Render, new FocusTextBox(MoveTextBoxInFocus));
            }
        }

        private void TextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                _textBox.Text = _oldText;
                IsInEditMode = false;
            }
            else if (e.Key == Key.Enter)
            {
                IsInEditMode = false;
            }
        }

        private void TextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            IsInEditMode = false;
        }

        private void EditableTabHeaderControlMouseDoubleClick
            (object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                SetEditMode(true);
            }
        }

        #region Nested type: FocusTextBox

        private delegate void FocusTextBox();

        #endregion
    }
}