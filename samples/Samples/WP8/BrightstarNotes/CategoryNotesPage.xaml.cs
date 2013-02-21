using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using BrightstarNotes.DataModel;
using Microsoft.Phone.Controls;

namespace BrightstarNotes
{
    public partial class CategoryNotesPage : PhoneApplicationPage
    {
        private string _categoryId;

        public CategoryNotesPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            // Find the related Notes from the selected Category
            string selectedCategoryId = "";
            if (NavigationContext.QueryString.TryGetValue("selectedCategory", out selectedCategoryId))
            {
                _categoryId = selectedCategoryId;
                INoteCategory category =
                    App.ViewModel.GetCategory(selectedCategoryId);
                DataContext = category;
            }
        }

        /// <summary>
        /// Naviages to the Note Edit Page when a Note is selected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditSelectedNote(object sender, SelectionChangedEventArgs e)
        {
            if (CategoryNotesListBox.SelectedIndex == -1)
            {
                return;
            }
            NavigationService.Navigate(new Uri("/EditPage.xaml?selectedItem=" + ((INote)CategoryNotesListBox.SelectedItem).Id,
                                               UriKind.Relative));
            CategoryNotesListBox.SelectedIndex = -1;
        }

        /// <summary>
        /// Deletes selected Note
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Delete_Note(object sender, MouseButtonEventArgs e)
        {
            var deleteButtonImage = (FrameworkElement)sender;
            App.ViewModel.DeleteObject(deleteButtonImage.DataContext);
            App.ViewModel.Refresh();
            DataContext = App.ViewModel.GetCategory(_categoryId);
            CategoryNotesListBox.SelectedIndex = -1;
            e.Handled = true;
        }
    }
}