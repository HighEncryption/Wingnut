namespace Wingnut.UI.Navigation
{
    using Wingnut.UI.ViewModels;

    public class UpsEnergyUsageNavigationViewModel : NavigationSectionViewModel
    {
        public UpsDeviceViewModel DeviceViewModel { get; }

        public UpsEnergyUsageNavigationViewModel(UpsDeviceViewModel deviceViewModel)
        {
            this.DeviceViewModel = deviceViewModel;
            this.NavigationHeader = "Energy Usage";
            this.PageHeader = "Energy Usage";
            this.Glyph = "\uE9D2";
        }
    }
}