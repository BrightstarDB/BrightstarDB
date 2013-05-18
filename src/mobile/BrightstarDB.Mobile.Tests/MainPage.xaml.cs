using System.Windows;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Silverlight.Testing;
using Microsoft.Silverlight.Testing.Harness;

namespace BrightstarDB.Mobile.Tests
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            SystemTray.IsVisible = false;
            var unitTestSettings = UnitTestSystem.CreateDefaultSettings();
            unitTestSettings.TestHarness.TestHarnessCompleted += HandleTestCompleted;
            var testPage = UnitTestSystem.CreateTestPage() as IMobileTestPage;
            BackKeyPress += (x, xe) => xe.Cancel = testPage.NavigateBack();
            (Application.Current.RootVisual as PhoneApplicationFrame).Content = testPage;
        }

        private void HandleTestCompleted(object sender, TestHarnessCompletedEventArgs e)
        {
            var testHarness = sender as UnitTestHarness;
            foreach (var result in testHarness.Results)
            {
                switch (result.Result)
                {
                    case TestOutcome.Passed:
                    case TestOutcome.Completed:
                        break;
                    default:
                        if (System.Diagnostics.Debugger.IsAttached)
                        {
                            System.Diagnostics.Debugger.Break();
                        }
                        break;
                }
            }
            BrightstarDB.Client.BrightstarService.Shutdown(false);
        }
    }
}