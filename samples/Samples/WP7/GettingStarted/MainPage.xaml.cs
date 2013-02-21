using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using GettingStarted.DataModel;
using Microsoft.Phone.Controls;

namespace GettingStarted
{
    public partial class MainPage : PhoneApplicationPage, INotifyPropertyChanged
    {
        private string _nextPageCategoryId = null;
        private int _dataSetSize = 100;
        private string _outputText = "Use the slider to change the dataset size";
        public String OutputText { get { return _outputText; } set { _outputText = value; NotifyPropertyChanged("OutputText"); } }
        public int DataSetSize { get { return _dataSetSize; } set { _dataSetSize = value; NotifyPropertyChanged("DataSetSize"); } }

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void HandleGenerateDataClick(object sender, RoutedEventArgs e)
        {
            var timer = new Stopwatch();
            timer.Start();
            _nextPageCategoryId = GenerateData(_dataSetSize);
            timer.Stop();
            OutputText = String.Format("Generated {0} items in {1} categories in {2}ms", _dataSetSize, _dataSetSize/10, timer.ElapsedMilliseconds);
            OutputText += "\n\nTap Next>> to continue...";
            Next.IsEnabled = true;
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            if (State.ContainsKey("TargetCategory")) State.Remove("TargetCategory");
            State.Add("TargetCategory", _nextPageCategoryId);
        }

        private void HandleNextClick(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/Query.xaml?category=" + Uri.EscapeDataString(_nextPageCategoryId), UriKind.Relative));
        }

        private string GenerateData(int count)
        {
            var context = new NotesContext(App.StoreConnectionString);
            var categories = new List<ICategory>();
            var rng = new Random();
            for(int i = 0; i < count/10;i++)
            {
                var category = context.Categories.Create();
                category.Label = String.Format("Generated Category #{0}", i + 1);
                categories.Add(category);
            }
            int catCount = categories.Count;

            for(int i = 0; i < count; i++)
            {
                var category = categories[rng.Next(catCount)];
                var note = context.Notes.Create();
                note.Category = category;
                note.Label = "Note " + DateTime.Now.Ticks;
                note.Content = "Lorem ipsum etc etc etc.";
            }
            context.SaveChanges();
            return categories[rng.Next(count/10)].CategoryId;
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Implementation of INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}