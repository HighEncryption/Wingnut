namespace Wingnut.UI.Controls
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    using Wingnut.UI.ViewModels;

    public enum DeviceTileMode
    {
        Undefined,
        AddDevice,
        RemoveDevice
    }

    /// <summary>
    /// Interaction logic for DeviceTile.xaml
    /// </summary>
    public partial class DeviceTile
    {
        public DeviceTile()
        {
            InitializeComponent();
        }

        private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var viewModel = (sender as Border)?.DataContext as DeviceReferenceViewModel;
            if (viewModel == null)
            {
                return;
            }

            if (e.ButtonState == MouseButtonState.Pressed)
            {
                viewModel.IsSelected = true;
            }
        }

        public static readonly DependencyProperty DisplayModeProperty =
            DependencyProperty.Register(
                "DisplayMode",
                typeof(DeviceTileMode),
                typeof(DeviceTile),
                new PropertyMetadata(default(DeviceTileMode)));

        public DeviceTileMode DisplayMode
        {
            get => (DeviceTileMode) GetValue(DisplayModeProperty);
            set => SetValue(DisplayModeProperty, value);
        }
    }
}
