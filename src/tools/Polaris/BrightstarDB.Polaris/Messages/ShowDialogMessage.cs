using System;
using System.Windows;

namespace BrightstarDB.Polaris.Messages
{
    public class ShowDialogMessage
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public MessageBoxImage Icon { get; set; }
        public MessageBoxButton Button { get; set; }
        public Action<MessageBoxResult> Callback { get; set; }

        public ShowDialogMessage(string title, string content, MessageBoxImage icon, MessageBoxButton button, Action<MessageBoxResult> callback = null)
        {
            Title = title;
            Content = content;
            Icon = icon;
            Button = button;
            Callback = callback;
        }
    }
}
