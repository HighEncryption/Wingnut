namespace Wingnut.UI.Controls
{
    using System.Windows.Controls;
    using System.Windows.Input;

    using Wingnut.UI.ViewModels;

    /// <summary>
    /// Interaction logic for AddDeviceTile.xaml
    /// </summary>
    public partial class AddDeviceTile
    {
        public AddDeviceTile()
        {
            InitializeComponent();
        }

        private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var viewModel = (sender as Border)?.DataContext as AddDeviceViewModel;
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
