using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;
using GalaSoft.MvvmLight;

namespace BrightstarDB.Polaris.ViewModel
{
    public class Store : ViewModelBase
    {
        private bool _isValid;
        private string _location;
        private static readonly char[] InvalidStoreNameChars = new[] {'\\', '/', ':', '?', '#'};

        public Store(Connection source, string location)
        {
            ValidationMessages = new ObservableCollection<string>();
            _location = location;
            Source = source;
            Validate();
        }

        public string Location
        {
            get { return _location; }
            set
            {
                _location = value;
                RaisePropertyChanged("Location");
                Validate();
            }
        }

        public Connection Source { get; private set; }

        public ObservableCollection<String> ValidationMessages { get; set; }
        public bool IsValid
        {
            get { return _isValid; }
            private set { _isValid = value; RaisePropertyChanged("IsValid"); }
        }

        public void Create()
        {
            Source.CreateStore(this);
        }

        private void Validate()
        {
            ValidationMessages.Clear();
            if (String.IsNullOrEmpty(_location))
            {
                ValidationMessages.Add("Please enter a store name.");
            }
            else if (InvalidStoreNameChars.Any(x=>_location.Contains(x)))
            {
                ValidationMessages.Add("The store name may not contain any of the following characters: " + String.Join("",InvalidStoreNameChars));
            }
            IsValid = ValidationMessages.Count == 0;
        }

        public XDocument ExecuteSparql(string sparqlQueryString, CommitPointViewModel targetCommitPoint)
        {
            return Source.ExecuteQuery(this, sparqlQueryString, targetCommitPoint);
        }

        public IEnumerable<CommitPointViewModel> GetCommitPoints(DateTime latest, DateTime earliest, int skip, int take)
        {
            return Source.GetCommitPoints(this, latest, earliest, skip, take);
        }

        public IEnumerable<CommitPointViewModel> GetCommitPoints(int skip, int take)
        {
            return Source.GetCommitPoints(this, skip, take);
        }

        public void RevertToCommitPoint(CommitPointViewModel targetCommitPoint)
        {
            Source.RevertToCommitPoint(this, targetCommitPoint);
        }

        public void ExecuteUpdate(string sparqlUpdateString)
        {
            Source.ExecuteUpdate(this, sparqlUpdateString);
        }

        public StatisticsViewModel GetStatistics()
        {
            return Source.GetStatistics(this);
        }
    }
}