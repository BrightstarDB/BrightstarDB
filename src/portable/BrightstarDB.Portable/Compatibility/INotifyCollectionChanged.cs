using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrightstarDB.Portable.Compatibility
{
    /// <summary>
    /// A partially compatible implementation of System.Collections.Specialized.INotifyCollectionChanged.
    /// </summary>
    public interface INotifyCollectionChanged
    {
        event NotifyCollectionChangedEventHandler CollectionChanged;
    }

    /// <summary>
    /// A partially compatible implementation of System.Collections.Specialized.NotifyCollectionChangedEventArgs.
    /// </summary>
    /// <remarks>Only the creation of single item Add and Remove event args; and Reset events are supported</remarks>
    public class NotifyCollectionChangedEventArgs : EventArgs
    {
        public NotifyCollectionChangedAction Action { get; private set; }
        public IList NewItems { get; private set; } 
        public IList OldItems { get; private set; }
        public int NewStartingIndex { get; private set; } 
        public int OldStartingIndex { get; private set; }

        public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action)
        {
            Action = action;
        }

        public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, object item, int index)
        {
            Action = action;
            switch (action)
            {
                case NotifyCollectionChangedAction.Add:
                    NewItems = new List<object> {item};
                    NewStartingIndex = index;
                    break;
                case NotifyCollectionChangedAction.Remove:
                    OldItems = new List<object> {item};
                    OldStartingIndex = index;
                    break;
            }
        }
    }

    /// <summary>
    /// Compatibility layer implementation of the System.Collections.Specialized.NotifyCollectionChangedEventHandler delegate
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public delegate bool NotifyCollectionChangedEventHandler(object sender, NotifyCollectionChangedEventArgs args);

    /// <summary>
    /// Compatibility layer implementation of the System.Collections.Specialized.NotifyCollectionChangedAction enumeration
    /// </summary>
    /// <remarks>Only the Add, Remove and Reset enum values are supported</remarks>
    public enum NotifyCollectionChangedAction
    {
        Add,
        Remove,
        Reset
    }
}
