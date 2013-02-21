using System;
using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Navigation;
using BrightstarNotes.DataModel;
using Microsoft.Phone.Controls;

namespace BrightstarNotes
{
    public partial class EditPage : PhoneApplicationPage
    {
        private static string _noteId;
        private static string _categoryId;
        private static INote _note;
        private static INoteCategory _category;
        private static IList _selectedNotes;

        public EditPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            string selectedId = "";
            if (NavigationContext.QueryString.TryGetValue("selectedItem", out selectedId))
            {
                _noteId = selectedId;
            }
            _note = App.ViewModel.GetNote(_noteId);
            _category = _note.Category;

            string selectedCategoryId = "";
            if (NavigationContext.QueryString.TryGetValue("selectedCategory", out selectedCategoryId))
            {
                _categoryId = selectedCategoryId;
                _category = App.ViewModel.GetCategory(selectedCategoryId);
                _note.Category = _category;
            }

            if (_note != null)
            {
                DataContext = _note;
            }
            if (_category != null)
            {
                textCategory.Text = _category.Title;
                textCategory.IsEnabled = true;
            }

            RelatedNotesListBox.ItemsSource = App.ViewModel.AllNotes.OrderBy(t => t.Title);
            // Determine which linked notes to check
            foreach (INote note in RelatedNotesListBox.ItemsSource)
            {
                if (_selectedNotes == null)
                {
                    if (_note.LinkedNotes.Where(t => t.Id.Equals(note.Id)).Any())
                    {
                        RelatedNotesListBox.SelectedItems.Add(note);
                    }
                }
                else
                {
                    if (_selectedNotes.Contains(note))
                    {
                        RelatedNotesListBox.SelectedItems.Add(note);
                    }
                }
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            var noteToSave = (INote)DataContext;

            noteToSave.Title = textTitle.Text;
            noteToSave.Body = textNote.Text;
            noteToSave.Modified = DateTime.Now;
            noteToSave.Category = _category;
            noteToSave.LinkedNotes.Clear();
            if (RelatedNotesListBox.SelectedItems.Count > 0)
            {
                foreach (INote selected in RelatedNotesListBox.SelectedItems)
                {
                    noteToSave.LinkedNotes.Add(selected);
                }
            }
            App.ViewModel.Context.SaveChanges();

            NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
            _note = null;
            _selectedNotes = null;
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
            _note = null;
            _selectedNotes = null;
        }

        private void CategoryButton_Click(object sender, RoutedEventArgs e)
        {
            _note = (INote)DataContext;
            _note.Title = textTitle.Text;
            _note.Body = textNote.Text;
            _note.LinkedNotes.Clear();
            if (RelatedNotesListBox.SelectedItems.Count > 0)
            {
                foreach (INote selected in RelatedNotesListBox.SelectedItems)
                {
                    _note.LinkedNotes.Add(selected);
                }
            }
            _selectedNotes = RelatedNotesListBox.SelectedItems;
            NavigationService.Navigate(new Uri("/AddCategoryPage.xaml?editPage=true", UriKind.Relative));
        }
    }
}