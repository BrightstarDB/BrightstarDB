using System;

namespace BrightstarDB.Polaris.Messages
{
    public class ShowWindowMessage
    {
        public string Name { get; set; }
        public object ViewModel { get; set; }
        public Action<object> Continuation { get; set; }
    }
}
