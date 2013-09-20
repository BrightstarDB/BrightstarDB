using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using BrightstarDB.Client;
using GalaSoft.MvvmLight.Command;

namespace BrightstarDB.Polaris.ViewModel
{
    class StoreStatisticsViewModel : StoreOperationViewModel
    {
        private IJobInfo _statsUpdateJob;
        private Dispatcher _dispatcher;

        private StatisticsViewModel _statistics;

        public StatisticsViewModel Statistics
        {
            get { return _statistics; }
            set
            {
                _statistics = value;
                RaisePropertyChanged("Statistics");
            }
        }

        private string _msg;
        public string SummaryMessage { get { return _msg; }
            set
            {
                _msg = value;
                RaisePropertyChanged("SummaryMessage");
            }
        }

        public RelayCommand<RoutedEventArgs> UpdateStatsCommand { get; private set; }

        public StoreStatisticsViewModel(Store store) : base(store)
        {
            UpdateStatsCommand = new RelayCommand<RoutedEventArgs>(UpdateStats);
            Refresh();

        }

        private void UpdateStats(RoutedEventArgs e)
        {
            var button = e.Source as Button;
            if (button != null)
            {
                _dispatcher = button.Dispatcher;
                var client = BrightstarService.GetClient(Store.Source.ConnectionString);
                _statsUpdateJob = client.UpdateStatistics(Store.Location);
                _dispatcher.BeginInvoke(DispatcherPriority.SystemIdle,
                                        new TransactionViewModel.JobMonitorDelegate(CheckJobStatus));
            }
        }

        private void CheckJobStatus()
        {
            try
            {
                var client = BrightstarService.GetClient(Store.Source.ConnectionString);
                _statsUpdateJob = client.GetJobInfo(Store.Location, _statsUpdateJob.JobId);
                if (_statsUpdateJob.JobPending)
                {
                    SummaryMessage = "Updated job is queued for processing.";
                }
                if (_statsUpdateJob.JobStarted)
                {
                    SummaryMessage = String.IsNullOrEmpty(_statsUpdateJob.StatusMessage)
                                         ? "Job is running"
                                         : _statsUpdateJob.StatusMessage;
                }

                if (_statsUpdateJob.JobCompletedOk)
                {
                    SummaryMessage = "Statistics updated completed.";
                    Refresh();
                }
                else if (_statsUpdateJob.JobCompletedWithErrors)
                {
                    SummaryMessage = "Updated job failed.";
                }
                else
                {
                    _dispatcher.BeginInvoke(DispatcherPriority.SystemIdle,
                                            new TransactionViewModel.JobMonitorDelegate(CheckJobStatus));
                }
            }
            catch (Exception)
            {
                SummaryMessage = "Error retrieving job status information from the server.";
            }
        }
        private void Refresh()
        {
            ThreadPool.QueueUserWorkItem(GetStatistics);
        }

        private void GetStatistics(object o)
        {
            try
            {
                Statistics = Store.GetStatistics();
                if (Statistics == null)
                {
                    SummaryMessage = "No statistics available.";
                }
            }
            catch (Exception)
            {
                SummaryMessage = "Error retrieving statistics.";
            }
        }
    }
}
