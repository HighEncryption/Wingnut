namespace Wingnut.UI.Controls
{
    using System.Windows.Controls;
    using System.Windows.Input;

    using Wingnut.UI.Navigation;

    /// <summary>
    /// Interaction logic for NavigationListItem.xaml
    /// </summary>
    public partial class NavigationListItem
    {
        public NavigationListItem()
        {
            InitializeComponent();
        }

        private void HandleOnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var viewModel = (sender as Border)?.DataContext as NavigationSectionViewModel;
            if (viewModel == null)
            {
                return;
            }

            if (e.ButtonState == MouseButtonState.Pressed)
            {
                viewModel.IsSelected = true;
            }
        }
    }
}
