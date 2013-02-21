using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using BrightstarNotes.DataModel;
using Microsoft.Phone.Controls;
using System.Linq;

namespace BrightstarNotes
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();

            DataContext = App.ViewModel;

            Loaded += new RoutedEventHandler(MainPage_Loaded);
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!App.ViewModel.IsDataLoaded)
            {
                App.ViewModel.LoadData();
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (DataContext == null)
            {
                DataContext = App.ViewModel;
            }

            App.ViewModel.Refresh();
        }

        /// <summary>
        /// Naviages to the Note Edit Page when a Note is selected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditSelectedNote(object sender, SelectionChangedEventArgs e)
        {
            if (AllNotesListBox.SelectedIndex == -1)
            {
                return;
            }
            var selectedNote = (sender as ListBox).SelectedItem as INote;
            if (selectedNote != null)
            {
                // Navigate to EditPage passing on the Id for the selected Note
                NavigationService.Navigate(new Uri("/EditPage.xaml?selectedItem=" + selectedNote.Id, UriKind.Relative));
                AllNotesListBox.SelectedIndex = -1;
            }
        }

        /// <summary>
        /// Navigates from the Categories list to Notes contained in selected Category
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ViewNotesInCategory(object sender, SelectionChangedEventArgs e)
        {
            if (CategoriesNotesListBox.SelectedIndex == -1)
            {
                return;
            }
            var selectedCategory = (sender as ListBox).SelectedItem as INoteCategory;
            if (selectedCategory != null)
            {
                NavigationService.Navigate(new Uri("/CategoryNotesPage.xaml?selectedCategory=" + selectedCategory.Id,
                                                   UriKind.Relative));
                CategoriesNotesListBox.SelectedIndex = -1;
            }
        }

        /// <summary>
        /// Performs search across all notes when text is entered in search box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = ((TextBox)sender).Text;
            if (string.IsNullOrWhiteSpace(searchText))
            {
                // Reset the search results and hide search results message
                AllNotesListBox.ItemsSource = App.ViewModel.AllNotes;
                SearchResultsMessage.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Search in Title and Body of all notes for search text
                AllNotesListBox.ItemsSource = App.ViewModel.AllNotes.Where(t => (t.Title.Contains(searchText) || t.Body.Contains(searchText))).ToList();
                SearchResultsMessage.Text = string.Format("Showing notes containing \"{0}\"", searchText);
                SearchResultsMessage.Visibility = Visibility.Visible;
            }
        }

        #region Add and Delete buttons

        /// <summary>
        /// Navigates to the Add Note Page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddNoteButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/AddPage.xaml", UriKind.Relative));

            AllNotesListBox.SelectedIndex = -1;
        }

        /// <summary>
        /// Deletes selected Note
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Delete_Note(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            AllNotesListBox.SelectedIndex = -1;
            CategoriesNotesListBox.SelectedIndex = -1;
            var deleteButtonImage = (FrameworkElement)sender;
            App.ViewModel.DeleteObject(deleteButtonImage.DataContext);
            App.ViewModel.Refresh();
            e.Handled = true;
        }

        /// <summary>
        /// Deletes selected Category (but not linked Notes)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Delete_Category(object sender, MouseButtonEventArgs e)
        {
            AllNotesListBox.SelectedIndex = -1;
            CategoriesNotesListBox.SelectedIndex = -1;
            var deleteButtonImage = (FrameworkElement)sender;
            App.ViewModel.DeleteObject(deleteButtonImage.DataContext);
            App.ViewModel.Refresh();
            e.Handled = true;
        }
        #endregion
    }
}