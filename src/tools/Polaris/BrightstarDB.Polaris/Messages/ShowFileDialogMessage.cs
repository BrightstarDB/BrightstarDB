using System;

namespace BrightstarDB.Polaris.Messages
{
    internal class ShowFileDialogMessage
    {
        /// <summary>
        /// Set to true for a save dialog, false for an open dialog
        /// </summary>
        public bool IsSave { get; set; }

        public string Title { get; set; }
        public string FileName { get; set; }
        public string DefaultExt { get; set; }
        public string Filter { get; set; }
        public string Directory { get; set; }

        /// <summary>
        /// Callback invoked with the selected file name if the user does not cancel the dialog
        /// </summary>
        public Action<string> Continuation { get; set; }
    }
}
