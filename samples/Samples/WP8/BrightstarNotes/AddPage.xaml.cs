using System;
using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Navigation;
using BrightstarNotes.DataModel;
using Microsoft.Phone.Controls;

namespace BrightstarNotes
{
    public partial class AddPage : PhoneApplicationPage
    {
        private INoteCategory _category;
        private static INote _note;
        private static IList _selectedNotes;

        public AddPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Check if a category has been selected from the Category chooser page and retrieve it
            string selectedCategory = "";
            if (NavigationContext.QueryString.TryGetValue("selectedCategory", out selectedCategory))
            {
                _category = App.ViewModel.Context.NoteCategories.Where(t => t.Id.Equals(selectedCategory)).First();
            }
            // If there is a cached Note then reuse the values
            if (_note != null)
            {
                textNote.Text = _note.Body;
                textTitle.Text = _note.Title;
            }
            // If there is a selected Category then reuse the values
            if (_category != null)
            {
                textCategory.Text = _category.Title;
                textCategory.IsEnabled = true;
            }
            if (App.ViewModel.Context.Notes.FirstOrDefault() != null)
            {
                NoNotesMessage.Visibility = Visibility.Collapsed;
                // Fill the Related Notes selector and pre-select Notes in the _selectedNotes cache
                RelatedNotesListBox.ItemsSource = App.ViewModel.Context.Notes.OrderBy(t => t.Title);
                if (_selectedNotes != null)
                {
                    foreach (INote note in RelatedNotesListBox.ItemsSource)
                    {
                        if (_selectedNotes.Contains(note))
                        {
                            RelatedNotesListBox.SelectedItems.Add(note);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Saves the Note and Category and any related selected Notes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveButton_Click(object sender, EventArgs e)
        {
            var note = App.ViewModel.Context.Notes.Create();
            note.Title = textTitle.Text;
            note.Body = textNote.Text;
            note.Modified = DateTime.Now;

            // Check if a category was selected
            if (_category != null)
            {
                note.Category = _category;
            }
            if (RelatedNotesListBox.SelectedItems.Count > 0)
            {
                // Add the selected Notes to the LinkedNotes list
                foreach (INote selected in RelatedNotesListBox.SelectedItems)
                {
                    note.LinkedNotes.Add(selected);
                }
            }
            App.ViewModel.Context.SaveChanges();
            ClearNotesCache();
            NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
        }

        /// <summary>
        /// Navigates to the MainPage
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelButton_Click(object sender, EventArgs e)
        {
            ClearNotesCache();
            NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
        }

        /// <summary>
        /// Navigates to the Category Selection page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CategoryButton_Click(object sender, RoutedEventArgs e)
        {
            _note = new Note() { Title = textTitle.Text, Body = textNote.Text };
            _selectedNotes = RelatedNotesListBox.SelectedItems;
            NavigationService.Navigate(new Uri("/AddCategoryPage.xaml", UriKind.Relative));
        }

        /// <summary>
        /// Clears _note and _selectedNotes cache
        /// </summary> 
        private void ClearNotesCache()
        {
            _note = null;
            _selectedNotes = null;
        }
    }
}