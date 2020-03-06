namespace Wingnut.UI.Navigation
{
    using Wingnut.UI.ViewModels;

    public class UpsSettingsNavigationViewModel : NavigationSectionViewModel
    {
        public UpsDeviceViewModel DeviceViewModel { get; }

        public UpsSettingsNavigationViewModel(UpsDeviceViewModel deviceViewModel)
        {
            this.DeviceViewModel = deviceViewModel;
            this.NavigationHeader = "Settings";
            this.PageHeader = "Settings";
            this.Glyph = "\uE713";
        }
    }
}