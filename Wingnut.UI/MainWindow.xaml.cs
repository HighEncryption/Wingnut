namespace Wingnut.UI
{
    using System.Windows.Controls;
    using System.Windows.Input;

    using Wingnut.UI.Navigation;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;

            var viewModel = border?.DataContext as NavigationSectionViewModel;
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
