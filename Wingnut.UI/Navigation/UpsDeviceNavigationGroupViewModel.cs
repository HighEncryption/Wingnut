namespace Wingnut.UI.Navigation
{
    using Wingnut.UI.ViewModels;

    public class UpsDeviceNavigationGroupViewModel : NavigationSectionGroupViewModel
    {
        public UpsDeviceViewModel DeviceViewModel { get; }

        public UpsDeviceNavigationGroupViewModel(UpsDeviceViewModel deviceViewModel)
        {
            this.DeviceViewModel = deviceViewModel;

            this.NavigationHeader = deviceViewModel.Ups.Name;
            this.PageHeader = deviceViewModel.Ups.Name;

            this.DeviceViewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(UpsDeviceViewModel.Ups.Name))
                {
                    this.NavigationHeader = deviceViewModel.Ups.Name;
                }
            };

            this.Sections.Add(new UpsStatusNavigationViewModel(deviceViewModel));
            this.Sections.Add(new UpsNotificationsNavigationViewModel(deviceViewModel));
            this.Sections.Add(new UpsEnergyUsageNavigationViewModel(deviceViewModel));
            this.Sections.Add(new UpsSettingsNavigationViewModel(deviceViewModel));
        }
    }
}