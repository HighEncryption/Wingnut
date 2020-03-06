namespace Wingnut.UI.Navigation
{
    using Wingnut.UI.ViewModels;

    public class UpsNotificationsNavigationViewModel : NavigationSectionViewModel
    {
        public UpsDeviceViewModel DeviceViewModel { get; }

        public UpsNotificationsNavigationViewModel(UpsDeviceViewModel deviceViewModel)
        {
            this.DeviceViewModel = deviceViewModel;
            this.NavigationHeader = "Notifications";
            this.PageHeader = "Notifications";
            this.Glyph = "\uEC42";
        }
    }
}