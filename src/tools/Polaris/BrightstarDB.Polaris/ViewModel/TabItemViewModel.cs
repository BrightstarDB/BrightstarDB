using System.Collections.Generic;
using System.Windows.Controls;
using BrightstarDB.Polaris.Messages;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;

namespace BrightstarDB.Polaris.ViewModel
{
    public class TabItemViewModel : ViewModelBase
    {
        public string Title { get; set; }

        public Control Content { get; set; }
        public List<string> Toolbars { get; private set; }

        public TabItemViewModel(string title, Control content)
        {
            Title = title;
            Content = content;
            NotifyViewCommand = new RelayCommand<string>(action=>NotifyView(action));
            CloseCommand = new RelayCommand(OnClose);
            ActivateCommand = new RelayCommand(OnActivate);
            Toolbars = new List<string>();

        }

        public RelayCommand<string> NotifyViewCommand { get; private set; }
        public RelayCommand CloseCommand { get; private set; }
        public RelayCommand ActivateCommand { get; private set; }

        public object NotifyView(string action)
        {
            var msg = new TabItemActionMessage {Action = action};
            Messenger.Default.Send(msg);
            return null;
        }

        public virtual bool HandleAppExit()
        {
            return true;
        }

        private void OnClose()
        {
            Messenger.Default.Send(new CloseTabMessage(this));
        }

        private void OnActivate()
        {
            Messenger.Default.Send(new SelectTabMessage(this));
        }
    }
}
