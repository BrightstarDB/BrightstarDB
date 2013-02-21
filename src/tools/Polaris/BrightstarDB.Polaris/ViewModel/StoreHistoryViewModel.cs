using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using BrightstarDB.Polaris.Configuration;
using GalaSoft.MvvmLight.Command;

namespace BrightstarDB.Polaris.ViewModel
{
    public class StoreHistoryViewModel : StoreOperationViewModel
    {
        private bool _hasMoreCommitPoints;
        private DateTime? _dtFilterFrom;
        private DateTime? _dtFilterTo;

        public ObservableCollection<CommitPointViewModel> CommitPoints { get; private set; }

        public SparqlQueryViewModel HistoricalQueryViewModel { get; private set; }

        public DateTime? DateTimeFilterFrom
        {
            get { return _dtFilterFrom; }
            set { _dtFilterFrom = value; Refresh(); }
        }

        public DateTime? DateTimeFilterTo
        {
            get { return _dtFilterTo; }
            set { _dtFilterTo = value; Refresh(); }
        }

        public RelayCommand ExecuteCommand { get; private set; }
        public RelayCommand RefreshCommand { get; private set; }
        public RelayCommand RevertCommand { get; private set; }
        public RelayCommand MoreCommitPointsCommand { get; private set; }
        public bool HasMoreCommitPoints { get { return _hasMoreCommitPoints; } set { _hasMoreCommitPoints = value; RaisePropertyChanged("HasMoreCommitPoints"); } }

        public StoreHistoryViewModel(Store store, IEnumerable<PrefixConfiguration> prefixes) : base(store)
        {
            CommitPoints = new ObservableCollection<CommitPointViewModel>(store.GetCommitPoints(0, 20));
            HasMoreCommitPoints = CommitPoints.Count == 20;
            HistoricalQueryViewModel = new SparqlQueryViewModel(store, prefixes);
            ExecuteCommand = HistoricalQueryViewModel.ExecuteCommand;
            RefreshCommand = new RelayCommand(Refresh);
            RevertCommand = new RelayCommand(Revert);
            MoreCommitPointsCommand = new RelayCommand(MoreCommitPoints);
        }

        public void Refresh()
        {
            if (DateTimeFilterFrom == null && DateTimeFilterTo == null)
            {
                var targetCount = Math.Max(100, CommitPoints.Count);
                CommitPoints.Clear();
                foreach (var commitPoint in Store.GetCommitPoints(0, targetCount))
                {
                    CommitPoints.Add(commitPoint);
                }
                HasMoreCommitPoints = CommitPoints.Count == targetCount;
            } else
            {
                DateTime latest, earliest;
                GetDateTimeFilter(out latest, out earliest);
                CommitPoints.Clear();
                foreach(var commitPoint in Store.GetCommitPoints(latest, earliest, 0, 100))
                {
                    CommitPoints.Add(commitPoint);
                }
                HasMoreCommitPoints = CommitPoints.Count == 100;
            }
        }

        private void GetDateTimeFilter(out DateTime latest, out DateTime earliest)
        {
            if (DateTimeFilterTo == null && DateTimeFilterFrom == null)
            {
                latest = DateTime.Now;
                earliest = DateTime.MinValue;
            }
            else
            {
                if (DateTimeFilterTo == null)
                {
                    latest = DateTimeFilterFrom.Value;
                    earliest = DateTime.MinValue;
                }
                else if (DateTimeFilterFrom == null)
                {
                    latest = DateTime.Now;
                    earliest = DateTimeFilterTo.Value;
                }
                else
                {
                    if (DateTimeFilterFrom.Value > DateTimeFilterTo.Value)
                    {
                        latest = DateTimeFilterFrom.Value;
                        earliest = DateTimeFilterTo.Value;
                    }
                    else
                    {
                        latest = DateTimeFilterTo.Value;
                        earliest = DateTimeFilterFrom.Value;
                    }
                }
            }
        }

        public void MoreCommitPoints()
        {
            var lastCommitPoint = CommitPoints[CommitPoints.Count - 1];
            int foundCount = 0;
            if (DateTimeFilterFrom == null && DateTimeFilterTo == null)
            {
                foreach (var commitPoint in Store.GetCommitPoints(CommitPoints.Count, 100))
                {
                    foundCount++;
                    if (commitPoint.CommitTime < lastCommitPoint.CommitTime)
                    {
                        CommitPoints.Add(commitPoint);
                    }
                }
                if (CommitPoints[CommitPoints.Count - 1].Id == lastCommitPoint.Id)
                {
                    // We didn't add any new commit points. This indicates that a lot of new commit points have been added to the store recently so we should do a full update
                    Refresh();
                }
                else
                {
                    HasMoreCommitPoints = (foundCount == 100);
                }
            }
            else
            {
                DateTime latest, earliest;
                GetDateTimeFilter(out latest, out earliest);
                var initialCount = CommitPoints.Count;
                foreach (var commitPoint in Store.GetCommitPoints(latest, earliest, CommitPoints.Count, 100))
                {
                    foundCount++;
                    CommitPoints.Add(commitPoint);
                }
                HasMoreCommitPoints = (CommitPoints.Count - initialCount) == 100;
            }
        }

        private void Revert()
        {
            if (HistoricalQueryViewModel.TargetCommitPoint == null)
            {
                MessageBox.Show("Please select the commit point you want to revert to.", "No commit point selected",
                                MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            var result = MessageBox.Show(
                string.Format(
                    "Are you sure you want to revert the state of the store to the same state as commit point {0} (ID: {1}) ?",
                    HistoricalQueryViewModel.TargetCommitPoint.CommitTime, HistoricalQueryViewModel.TargetCommitPoint.Id),
                "Revert to Commit Point",
                MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    Store.RevertToCommitPoint(HistoricalQueryViewModel.TargetCommitPoint);
                    MessageBox.Show("Store revert completed successfully.", "Revert completed", MessageBoxButton.OK,
                                    MessageBoxImage.Information);
                    Refresh();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(String.Format("Store revert failed. Cause: {0}", ex.Message),
                                    "Store revert failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
