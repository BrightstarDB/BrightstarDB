using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using Calendar = System.Windows.Controls.Calendar;

namespace BrightstarDB.Polaris.Controls
{
    public enum DateTimePickerFormat
    {
        Long,
        Short,
        Time,
        Custom
    }

    [DefaultBindingProperty("Value")]
    public class DateTimePicker : Control
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof (DateTime?), typeof (DateTimePicker),
                                        new FrameworkPropertyMetadata(DateTime.Now,
                                                                      FrameworkPropertyMetadataOptions.
                                                                          BindsTwoWayByDefault, OnValueChanged,
                                                                      CoerceValue, true,
                                                                      UpdateSourceTrigger.PropertyChanged));

        private readonly string _defaultFormat =
            CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern + " " +
            CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern;

        internal TextBox TextBox;
        private BlockManager _blockManager;
        private Calendar _calendar;
        private CheckBox _checkBox;
        private string _customFormat;
        private DateTimePickerFormat _format;
        private Popup _popUp;
        private TextBlock _textBlock;

        public DateTimePicker()
        {
            Initialize();
            _blockManager = new BlockManager(this, FormatString);
        }

        //private string _defaultFormat = "MM/dd/yyyy hh:mm:ss tt";

        [Category("DateTimePicker")]
        public bool ShowCheckBox
        {
            get { return _checkBox.Visibility == Visibility.Visible ? true : false; }
            set
            {
                if (value)
                    _checkBox.Visibility = Visibility.Visible;
                else
                {
                    _checkBox.Visibility = Visibility.Collapsed;
                    Checked = true;
                }
            }
        }

        [Category("DateTimePicker")]
        public bool ShowDropDown
        {
            get { return _textBlock.Visibility == Visibility.Visible ? true : false; }
            set {
                _textBlock.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        [Category("DateTimePicker")]
        public bool Checked
        {
            get { return _checkBox.IsChecked.HasValue ? _checkBox.IsChecked.Value : false; }
            set { _checkBox.IsChecked = value; }
        }

        [Category("DateTimePicker")]
        private string FormatString
        {
            get
            {
                switch (Format)
                {
                    case DateTimePickerFormat.Long:
                        return "dddd, MMMM dd, yyyy";
                    case DateTimePickerFormat.Short:
                        return "M/d/yyyy";
                    case DateTimePickerFormat.Time:
                        return "h:mm:ss tt";
                    case DateTimePickerFormat.Custom:
                        return string.IsNullOrEmpty(CustomFormat) ? _defaultFormat : CustomFormat;
                    default:
                        return _defaultFormat;
                }
            }
        }

        [Category("DateTimePicker")]
        public string CustomFormat
        {
            get { return _customFormat; }
            set
            {
                _customFormat = value;
                _blockManager = new BlockManager(this, FormatString);
            }
        }

        [Category("DateTimePicker")]
        public DateTimePickerFormat Format
        {
            get { return _format; }
            set
            {
                _format = value;
                _blockManager = new BlockManager(this, FormatString);
            }
        }

        [Category("DateTimePicker")]
        public DateTime? Value
        {
            get
            {
                if (!Checked) return null;
                return (DateTime?) GetValue(ValueProperty);
            }
            set { SetValue(ValueProperty, value); }
        }

        internal DateTime InternalValue
        {
            get
            {
                DateTime? value = Value;
                if (value.HasValue) return value.Value;
                return DateTime.MinValue;
            }
        }

        // Using a DependencyProperty as the backing store for TheDate.  This enables animation, styling, binding, etc...

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DateTimePicker)
            {
                (d as DateTimePicker)._blockManager.Render();
            }
        }

        private static object CoerceValue(DependencyObject d, object value)
        {
            return value;
        }

        private void Initialize()
        {
            Template = GetTemplate();
            ApplyTemplate();
            _checkBox = (CheckBox) Template.FindName("checkBox", this);
            TextBox = (TextBox) Template.FindName("textBox", this);
            _textBlock = (TextBlock) Template.FindName("textBlock", this);
            _calendar = new Calendar();
            _popUp = new Popup {PlacementTarget = TextBox, Placement = PlacementMode.Bottom, Child = _calendar};
            _checkBox.Checked += HandleCheckBoxChecked;
            _checkBox.Unchecked += HandleCheckBoxChecked;
            MouseWheel += HandleMouseWheel;
            Focusable = false;
            TextBox.Cursor = Cursors.Arrow;
            TextBox.AllowDrop = false;
            TextBox.GotFocus += HandleTextBoxGotFocus;
            TextBox.PreviewMouseUp += HandleTextBoxPreviewMouseUp;
            TextBox.PreviewKeyDown += HandleTextBoxPreviewKeyDown;
            TextBox.ContextMenu = null;
            TextBox.IsEnabled = Checked;
            TextBox.IsReadOnly = true;
            TextBox.IsReadOnlyCaretVisible = false;
            _textBlock.MouseLeftButtonDown += HandleTextBlockMouseLeftButtonDown;
            _calendar.SelectedDatesChanged += HandleCalendarSelectedDatesChanged;
        }

        private void HandleTextBlockMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _popUp.IsOpen = !(_popUp.IsOpen);
        }

        private void HandleCheckBoxChecked(object sender, RoutedEventArgs e)
        {
            TextBox.IsEnabled = Checked;
            Value = Checked ? InternalValue : (DateTime?) null;
        }

        private void HandleMouseWheel(object sender, MouseWheelEventArgs e)
        {
            _blockManager.Change(((e.Delta < 0) ? -1 : 1), true);
        }

        private void HandleTextBoxGotFocus(object sender, RoutedEventArgs e)
        {
            _blockManager.ReSelect();
        }

        private void HandleTextBoxPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            _blockManager.ReSelect();
        }

        private void HandleTextBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            var b = (byte) e.Key;

            if (e.Key == Key.Left)
                _blockManager.Left();
            else if (e.Key == Key.Right)
                _blockManager.Right();
            else if (e.Key == Key.Up)
                _blockManager.Change(1, true);
            else if (e.Key == Key.Down)
                _blockManager.Change(-1, true);
            if (b >= 34 && b <= 43)
                _blockManager.ChangeValue(b - 34);
            if (e.Key != Key.Tab)
                e.Handled = true;
        }

        private void HandleCalendarSelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is Calendar)
            {
                Checked = true;
                ((Popup) (sender as Calendar).Parent).IsOpen = false;
                var selectedDate = (DateTime) e.AddedItems[0];
                Value = new DateTime(selectedDate.Year, selectedDate.Month, selectedDate.Day, InternalValue.Hour,
                                     InternalValue.Minute, InternalValue.Second);
                _blockManager.Render();
            }
        }

        private static ControlTemplate GetTemplate()
        {
            return
                (ControlTemplate)
                XamlReader.Parse(
                    @"
        <ControlTemplate  xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                          xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
            <Border BorderBrush=""Black"" BorderThickness=""0"" CornerRadius=""1"">
                <StackPanel Orientation=""Horizontal"" VerticalAlignment=""Center"" HorizontalAlignment=""Left"" Background=""White"">
                    <CheckBox Name=""checkBox"" VerticalAlignment=""Center"" />
                    <TextBox Name=""textBox"" BorderThickness=""0""/>
                    <TextBlock Name=""textBlock"" Text=""▼""/>
                </StackPanel>
            </Border>
        </ControlTemplate>");
        }

        public override string ToString()
        {
            return InternalValue.ToString();
        }

    }

    internal class BlockManager
    {
        private readonly string[] _supportedFormats = new[]
                                                          {
                                                              "yyyy", "MMMM", "dddd",
                                                              "yyy", "MMM", "ddd",
                                                              "yy", "MM", "dd",
                                                              "y", "M", "d",
                                                              "HH", "H", "hh", "h",
                                                              "mm", "m",
                                                              "ss", "s",
                                                              "tt", "t",
                                                              "fff", "ff", "f",
                                                              "K", "g"
                                                          };

        internal DateTimePicker DateTimePicker;
        private List<Block> _blocks;
        private string _format;
        private Block _selectedBlock;
        private int _selectedIndex;

        public BlockManager(DateTimePicker dateTimePicker, string format)
        {
            DateTimePicker = dateTimePicker;
            _format = format;
            DateTimePicker.LostFocus += DateTimePickerLostFocus;
            _blocks = new List<Block>();
            InitBlocks();
        }

        public event EventHandler NeglectProposed;

        private void InitBlocks()
        {
            foreach (string f in _supportedFormats)
                _blocks.AddRange(GetBlocks(f));
            _blocks = _blocks.OrderBy(a => a.Index).ToList();
            _selectedBlock = _blocks[0];
            Render();
        }

        internal void Render()
        {
            int accum = 0;
            var sb = new StringBuilder(_format);
            foreach (Block b in _blocks)
                b.Render(ref accum, sb);
            DateTimePicker.TextBox.Text = _format = sb.ToString();
            Select(_selectedBlock);
        }

        private IEnumerable<Block> GetBlocks(string pattern)
        {
            var bCol = new List<Block>();

            int index = -1;
            while ((index = _format.IndexOf(pattern, ++index)) > -1)
                bCol.Add(new Block(this, pattern, index));
            _format = _format.Replace(pattern, (0).ToString().PadRight(pattern.Length, '0'));
            return bCol;
        }

        internal void ChangeValue(int p)
        {
            _selectedBlock.Proposed = p;
            Change(_selectedBlock.Proposed, false);
        }

        internal void Change(int value, bool upDown)
        {
            DateTimePicker.Value = _selectedBlock.Change(DateTimePicker.InternalValue, value, upDown);
            if (upDown)
                OnNeglectProposed();
            Render();
        }

        internal void Right()
        {
            if (_selectedIndex + 1 < _blocks.Count)
                Select(_selectedIndex + 1);
        }

        internal void Left()
        {
            if (_selectedIndex > 0)
                Select(_selectedIndex - 1);
        }

        private void DateTimePickerLostFocus(object sender, RoutedEventArgs e)
        {
            OnNeglectProposed();
        }

        protected virtual void OnNeglectProposed()
        {
            EventHandler temp = NeglectProposed;
            if (temp != null)
            {
                temp(this, EventArgs.Empty);
            }
        }

        internal void ReSelect()
        {
            foreach (Block b in _blocks)
                if ((b.Index <= DateTimePicker.TextBox.SelectionStart) &&
                    ((b.Index + b.Length) >= DateTimePicker.TextBox.SelectionStart))
                {
                    Select(b);
                    return;
                }
            Block bb = _blocks.Where(a => a.Index < DateTimePicker.TextBox.SelectionStart).LastOrDefault();
            if (bb == null) Select(0);
            else Select(bb);
        }

        private void Select(int blockIndex)
        {
            if (_blocks.Count > blockIndex)
                Select(_blocks[blockIndex]);
        }

        private void Select(Block block)
        {
            if (_selectedBlock != block)
            {
                OnNeglectProposed();
            }
            _selectedIndex = _blocks.IndexOf(block);
            _selectedBlock = block;
            DateTimePicker.TextBox.Select(block.Index, block.Length);
        }
    }

    internal class Block
    {
        private readonly BlockManager _blockManager;
        private readonly int _maxLength;
        private string _proposed;

        public Block(BlockManager blockManager, string pattern, int index)
        {
            _blockManager = blockManager;
            _blockManager.NeglectProposed += HandleNeglectProposed;
            Pattern = pattern;
            Index = index;
            Length = Pattern.Length;
            _maxLength = GetMaxLength(Pattern);
        }

        internal string Pattern { get; set; }
        internal int Index { get; set; }
        internal int Length { get; set; }

        internal int Proposed
        {
            get
            {
                string p = _proposed;
                return int.Parse(p.PadLeft(Length, '0'));
            }
            set
            {
                if (_proposed != null && _proposed.Length >= _maxLength)
                    _proposed = value.ToString();
                else
                    _proposed = string.Format("{0}{1}", _proposed, value);
            }
        }

        private static int GetMaxLength(string p)
        {
            switch (p)
            {
                case "y":
                case "M":
                case "d":
                case "h":
                case "m":
                case "s":
                case "H":
                    return 2;
                case "yyy":
                    return 4;
                default:
                    return p.Length;
            }
        }

        private void HandleNeglectProposed(object sender, EventArgs e)
        {
            _proposed = null;
        }

        internal DateTime Change(DateTime dateTime, int value, bool upDown)
        {
            if (!upDown && !CanChange()) return dateTime;
            int y = dateTime.Year;
            int m = dateTime.Month;
            int d = dateTime.Day;
            int h = dateTime.Hour;
            int n = dateTime.Minute;
            int s = dateTime.Second;

            if (Pattern.Contains("y"))
                y = ((upDown) ? dateTime.Year + value : value);
            else if (Pattern.Contains("M"))
                m = ((upDown) ? dateTime.Month + value : value);
            else if (Pattern.Contains("d"))
                d = ((upDown) ? dateTime.Day + value : value);
            else if (Pattern.Contains("h") || Pattern.Contains("H"))
                h = ((upDown) ? dateTime.Hour + value : value);
            else if (Pattern.Contains("m"))
                n = ((upDown) ? dateTime.Minute + value : value);
            else if (Pattern.Contains("s"))
                s = ((upDown) ? dateTime.Second + value : value);
            else if (Pattern.Contains("t"))
                h = ((h < 12) ? (h + 12) : (h - 12));

            if (y > 9999) y = 1;
            if (y < 1) y = 9999;
            if (m > 12) m = 1;
            if (m < 1) m = 12;
            if (d > DateTime.DaysInMonth(y, m)) d = 1;
            if (d < 1) d = DateTime.DaysInMonth(y, m);
            if (h > 23) h = 0;
            if (h < 0) h = 23;
            if (n > 59) n = 0;
            if (n < 0) n = 59;
            if (s > 59) s = 0;
            if (s < 0) s = 59;

            return new DateTime(y, m, d, h, n, s);
        }

        private bool CanChange()
        {
            switch (Pattern)
            {
                case "MMMM":
                case "dddd":
                case "MMM":
                case "ddd":
                case "g":
                    return false;
                default:
                    return true;
            }
        }

        public override string ToString()
        {
            return string.Format("{0}, {1}", Pattern, Index);
        }

        internal void Render(ref int accum, StringBuilder sb)
        {
            Index += accum;

            string f = _blockManager.DateTimePicker.InternalValue.ToString(Pattern + ",").TrimEnd(',');
            sb.Remove(Index, Length);
            sb.Insert(Index, f);
            accum += f.Length - Length;

            Length = f.Length;
        }
    }
}