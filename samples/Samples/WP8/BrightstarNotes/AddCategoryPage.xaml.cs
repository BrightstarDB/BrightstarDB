using System;
using System.Windows;
using System.Windows.Controls;
using BrightstarNotes.DataModel;
using Microsoft.Phone.Controls;

namespace BrightstarNotes
{
    public partial class AddCategoryPage : PhoneApplicationPage
    {
        private bool _editPage;

        public AddCategoryPage()
        {
            InitializeComponent();
            CategoriesNotesListBox.SelectionChanged += SelectCategory;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            string editPage;
            if (NavigationContext.QueryString.TryGetValue("editPage", out editPage))
            {
                _editPage = bool.Parse(editPage);
            }
            if (App.ViewModel.Categories.Count == 0)
            {
                NoCategoriesMessage.Visibility = Visibility.Visible;
            }
            else
            {
                CategoriesNotesListBox.Visibility = Visibility.Visible;
                CategoriesNotesListBox.DataContext = App.ViewModel;
            }
        }

        /// <summary>
        /// Selects a category from the category list and returns to Edit or Add Note Page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectCategory(object sender, SelectionChangedEventArgs e)
        {
            if (CategoriesNotesListBox.SelectedIndex == -1)
            {
                return;
            }
            var category = (sender as ListBox).SelectedItem as INoteCategory;
            if (category != null)
            {
                if (_editPage)
                {
                    NavigationService.Navigate(
                        new Uri("/EditPage.xaml?selectedCategory=" + category.Id, UriKind.Relative));
                    CategoriesNotesListBox.SelectedIndex = -1;
                }
                else
                {
                    NavigationService.Navigate(
                        new Uri("/AddPage.xaml?selectedCategory=" + category.Id, UriKind.Relative));
                    CategoriesNotesListBox.SelectedIndex = -1;
                }
            }
        }

        /// <summary>
        /// Saves the newly created Category and returns to the Add or Edit Note Page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(textTitle.Text))
            {
                var category = App.ViewModel.Context.NoteCategories.Create();
                category.Title = textTitle.Text;
                App.ViewModel.Context.SaveChanges();
                App.ViewModel.Refresh();
                // pass on category Id by guid
                if (_editPage)
                {
                    NavigationService.Navigate(
                        new Uri("/EditPage.xaml?selectedCategory=" + category.Id, UriKind.Relative));
                }
                else
                {

                    NavigationService.Navigate(new Uri("/AddPage.xaml?selectedCategory=" + category.Id, UriKind.Relative));
                }
            }
        }
    }
}
