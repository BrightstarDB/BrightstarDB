using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public partial class Query : PhoneApplicationPage
    {
        public static readonly DependencyProperty TargetCategoryProperty =
            DependencyProperty.Register("TargetCategory", typeof (ICategory), typeof (Query), new PropertyMetadata(null));

        /// <summary>
        /// The category being queried
        /// </summary>
        public ICategory TargetCategory
        {
            get { return (ICategory) GetValue(TargetCategoryProperty); }
            set { SetValue(TargetCategoryProperty, value); }
        }

        public static readonly DependencyProperty TimingInfoProperty =
            DependencyProperty.Register("TimingInfo", typeof (string), typeof (Query), new PropertyMetadata(null));

        /// <summary>
        /// Display text informing the user how long the query took to run
        /// </summary>
        public string TimingInfo
        {
            get { return (string) GetValue(TimingInfoProperty); }
            set { SetValue(TimingInfoProperty, value); }
        }

        public static readonly DependencyProperty NotesProperty =
            DependencyProperty.Register("Notes", typeof (List<INote>), typeof (Query),
                                        new PropertyMetadata(new List<INote>(0)));

        /// <summary>
        /// The list of notes found in the target category of the query
        /// </summary>
        public List<INote> Notes
        {
            get { return (List<INote>) GetValue(NotesProperty); }
            set { SetValue(NotesProperty, value); }
        }

        public Query()
        {
            InitializeComponent();
            DataContext = this;
        }


        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (NavigationContext.QueryString.ContainsKey("category"))
            {
                // Navigating to this page kicks off the query.
                RetrieveCategory(NavigationContext.QueryString["category"]);
            }
        }

        /// <summary>
        /// Performs the query to retrieve the category object specified
        /// by <paramref name="categoryId"/> and all notes in that category.
        /// </summary>
        /// <param name="categoryId">The identifier of the category to retrieve</param>
        private void RetrieveCategory(string categoryId)
        {
            // Open the BrightstarDB database using the shared connection string
            var context = new NotesContext(App.StoreConnectionString);

            // LINQ query to locate a category by its identifier
            TargetCategory = context.Categories.Where(x => x.CategoryId == categoryId).FirstOrDefault();
            
            var timer = new Stopwatch();
            timer.Start();

            // Sample LINQ query for all notes in a specific category
            Notes =
                context.Notes.Where(n => n.Category.CategoryId== categoryId).ToList();
            timer.Stop();

            TimingInfo = String.Format("Query returned {0} notes in the category in {1}ms", Notes.Count,
                                       timer.ElapsedMilliseconds);
        }
    }
}