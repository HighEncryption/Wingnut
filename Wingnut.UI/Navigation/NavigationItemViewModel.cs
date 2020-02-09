namespace Wingnut.UI.Navigation
{
    using System.Collections.ObjectModel;
    using System.Diagnostics;

    using Wingnut.UI.Framework;
    using Wingnut.UI.ViewModels;

    public class NavigationItemViewModel : ViewModelBase
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string header;

        public string Header
        {
            get => this.header;
            set => this.SetProperty(ref this.header, value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool isSelected;

        public bool IsSelected
        {
            get => this.isSelected;
            set => this.SetProperty(ref this.isSelected, value);
        }
    }

    public class NavigationSectionViewModel : ViewModelBase
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string header;

        public string Header
        {
            get => this.header;
            set => this.SetProperty(ref this.header, value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string glyph;

        public string Glyph
        {
            get => this.glyph;
            set => this.SetProperty(ref this.glyph, value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool isSelected;

        public bool IsSelected
        {
            get => this.isSelected;
            set
            {
                if (this.SetProperty(ref this.isSelected, value) && value)
                {
                    App.Current.MainWindowViewModel.SelectedNavigationSection = this;
                }
            }
        }
    }

    public class UpsStatusNavigationViewModel : NavigationSectionViewModel
    {
        public UpsDeviceViewModel DeviceViewModel { get; }

        public UpsStatusNavigationViewModel(UpsDeviceViewModel deviceViewModel)
        {
            this.DeviceViewModel = deviceViewModel;
            this.Header = "Status";
            this.Glyph = "\uEC4A";
        }
    }

    public class UpsNotificationsNavigationViewModel : NavigationSectionViewModel
    {
        public UpsDeviceViewModel DeviceViewModel { get; }

        public UpsNotificationsNavigationViewModel(UpsDeviceViewModel deviceViewModel)
        {
            this.DeviceViewModel = deviceViewModel;
            this.Header = "Notifications";
            this.Glyph = "\uEC42";
        }
    }

    public class UpsEnergyUsageNavigationViewModel : NavigationSectionViewModel
    {
        public UpsDeviceViewModel DeviceViewModel { get; }

        public UpsEnergyUsageNavigationViewModel(UpsDeviceViewModel deviceViewModel)
        {
            this.DeviceViewModel = deviceViewModel;
            this.Header = "Energy Usage";
            this.Glyph = "\uE9D2";
        }
    }

    public class UpsSettingsNavigationViewModel : NavigationSectionViewModel
    {
        public UpsDeviceViewModel DeviceViewModel { get; }

        public UpsSettingsNavigationViewModel(UpsDeviceViewModel deviceViewModel)
        {
            this.DeviceViewModel = deviceViewModel;
            this.Header = "Energy Usage";
            this.Glyph = "\uE713";
        }
    }


    public class UpsDeviceNavigationViewModel : NavigationItemViewModel
    {
        public UpsDeviceViewModel DeviceViewModel { get; }

        private ObservableCollection<NavigationSectionViewModel> navigationSectionViewModels;

        public ObservableCollection<NavigationSectionViewModel> NavigationSectionViewModels =>
            this.navigationSectionViewModels ?? 
            (this.navigationSectionViewModels = new ObservableCollection<NavigationSectionViewModel>());

        public UpsDeviceNavigationViewModel(UpsDeviceViewModel deviceViewModel)
        {
            this.DeviceViewModel = deviceViewModel;

            this.NavigationSectionViewModels.Add(new UpsStatusNavigationViewModel(deviceViewModel));
            this.NavigationSectionViewModels.Add(new UpsNotificationsNavigationViewModel(deviceViewModel));
            this.NavigationSectionViewModels.Add(new UpsEnergyUsageNavigationViewModel(deviceViewModel));
            this.NavigationSectionViewModels.Add(new UpsSettingsNavigationViewModel(deviceViewModel));
        }
    }

    public class HomePageNavigationViewModel : NavigationSectionViewModel
    {
        public HomePageViewModel HomePageViewModel { get; }

        public HomePageNavigationViewModel(HomePageViewModel homePageViewModel)
        {
            this.HomePageViewModel = homePageViewModel;

            this.Header = "Windows GUI for Network UPS Tools";
        }
    }
}
