namespace Wingnut.UI.Navigation
{
    using Wingnut.UI.ViewModels;

    public class UpsDeviceNavigationGroupViewModel : NavigationSectionGroupViewModel
    {
        public UpsDeviceViewModel DeviceViewModel { get; }

        public UpsDeviceNavigationGroupViewModel(UpsDeviceViewModel deviceViewModel)
        {
            this.DeviceViewModel = deviceViewModel;

            this.NavigationHeader = deviceViewModel.UpsName;
            this.PageHeader = deviceViewModel.UpsName;

            this.DeviceViewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(UpsDeviceViewModel.UpsName))
                {
                    this.NavigationHeader = deviceViewModel.UpsName;
                }
            };

            this.Sections.Add(new UpsStatusNavigationViewModel(deviceViewModel));
            this.Sections.Add(new UpsNotificationsNavigationViewModel(deviceViewModel));
            this.Sections.Add(new UpsEnergyUsageNavigationViewModel(deviceViewModel));
            this.Sections.Add(new UpsSettingsNavigationViewModel(deviceViewModel));
        }
    }

    public class UpsStatusNavigationViewModel : NavigationSectionViewModel
    {
        public UpsDeviceViewModel Device { get; }

        public UpsStatusNavigationViewModel(UpsDeviceViewModel device)
        {
            this.Device = device;
            this.NavigationHeader = "Status";
            this.PageHeader = "Status";
            this.Glyph = "\uEC4A";
        }
    }

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

    public class UpsSettingsNavigationViewModel : NavigationSectionViewModel
    {
        public UpsDeviceViewModel DeviceViewModel { get; }

        public UpsSettingsNavigationViewModel(UpsDeviceViewModel deviceViewModel)
        {
            this.DeviceViewModel = deviceViewModel;
            this.NavigationHeader = "Energy Usage";
            this.PageHeader = "Energy Usage";
            this.Glyph = "\uE713";
        }
    }
}