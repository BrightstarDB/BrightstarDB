using System;
using BrightstarDB.Client;
using GalaSoft.MvvmLight;

namespace BrightstarDB.Polaris.ViewModel
{
    public class CommitPointViewModel : ViewModelBase
    {
        private static readonly string DateTimeFormat =
            System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern + " " +
            System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern;
        public ulong Id { get; private set; }
        public DateTime CommitTime { get; private set; }
        public Guid JobId { get; private set; }
        public string CommitTimeString { get { return CommitTime.ToString(DateTimeFormat); } }
        public CommitPointViewModel(ICommitPointInfo commitPointInfo)
        {
            CommitTime = commitPointInfo.CommitTime;
            Id = commitPointInfo.Id;
            JobId = commitPointInfo.JobId;
        }
    }
}