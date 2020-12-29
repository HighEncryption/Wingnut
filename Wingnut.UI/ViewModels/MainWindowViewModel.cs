namespace Wingnut.UI.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.ServiceModel;
    using System.Threading.Tasks;
    using System.Windows.Input;

    using Wingnut.Channels;
    using Wingnut.Data.Configuration;
    using Wingnut.Data.Models;
    using Wingnut.UI.Framework;

    public class MainWindowViewModel : ViewModelBase, IDisposable
    {
        //private ObservableCollection<NavigationSectionViewModel> navigationSections;

        //public ObservableCollection<NavigationSectionViewModel> NavigationSections =>
        //    this.navigationSections ?? (this.navigationSections = new ObservableCollection<NavigationSectionViewModel>());

        //[DebuggerBrowsable(DebuggerBrowsableState.Never)]
        //private NavigationSectionViewModel selectedNavigationSection;

        //public NavigationSectionViewModel SelectedNavigationSection
        //{
        //    get => this.selectedNavigationSection;
        //    set
        //    {
        //        var previousNavSection = this.selectedNavigationSection;

        //        if (this.SetProperty(ref this.selectedNavigationSection, value) &&
        //            previousNavSection != null)
        //        {
        //            previousNavSection.IsSelected = false;
        //        }
        //    } 
        //}

        private ObservableCollection<PageViewModel> pages;

        public ObservableCollection<PageViewModel> Pages =>
            this.pages ?? (this.pages = new ObservableCollection<PageViewModel>());

        private PageViewModel selectedPage;

        public PageViewModel SelectedPage
        {
            get => this.selectedPage;
            set
            {
                var previousPath = this.selectedPage;

                if (this.SetProperty(ref this.selectedPage, value) && previousPath != null)
                {
                    previousPath.IsSelected = false;
                }
            }
        }

        private bool isNavigationCollapsed;

        public bool IsNavigationCollapsed
        {
            get => this.isNavigationCollapsed;
            set => this.SetProperty(ref this.isNavigationCollapsed, value);
        }

        public ICommand ToggleNavigationPane { get; }

        private ObservableCollection<DeviceViewModel> deviceViewModels;

        public ObservableCollection<DeviceViewModel> DeviceViewModels =>
            this.deviceViewModels ?? (this.deviceViewModels = new ObservableCollection<DeviceViewModel>());

        private bool hasDevices;

        public bool HasDevices
        {
            get => this.hasDevices;
            set => this.SetProperty(ref this.hasDevices, value);
        }

        private DeviceViewModel selectedDevice;

        public DeviceViewModel SelectedDevice
        {
            get => this.selectedDevice;
            set => this.SetProperty(ref this.selectedDevice, value);
        }

        public MainWindowViewModel()
        {
            this.ToggleNavigationPane = new DelegatedCommand(
                this.ToggleNavigationPaneOnExecute);

            this.DeviceViewModels.CollectionChanged += 
                (s, a) => this.HasDevices = DeviceViewModels.Any();
        }

        private void ToggleNavigationPaneOnExecute(object obj)
        {
            this.IsNavigationCollapsed = !this.IsNavigationCollapsed;
        }

        private class CallbackClient : IManagementCallback
        {
            public void UpsDeviceAdded(Ups ups)
            {
                Task.Run(() =>
                {
                    App.Current.MainWindowViewModel.AddDeviceToService(ups);
                });
            }

            public void UpsDeviceChanged(Ups ups)
            {
                ups.Initialize();

                foreach (UpsDeviceViewModel viewModel in 
                    App.Current.MainWindowViewModel.DeviceViewModels.OfType<UpsDeviceViewModel>())
                {
                    if (viewModel.Ups.QualifiedName == ups.QualifiedName)
                    {
                        //viewModel.Update(ups);
                        viewModel.Ups.UpdateVariables(ups);
                        return;
                    }
                }
            }

            public void UpsDeviceRemoved(string serverName, string upsName)
            {
                DeviceViewModel deviceViewModel =
                    App.Current.MainWindowViewModel.DeviceViewModels.FirstOrDefault(
                        d => d.Device.Server.Name == serverName &&
                             d.Device.Name == upsName);

                var ups = deviceViewModel?.Device as Ups;

                if (ups == null)
                {
                    return;
                }

                App.Current.MainWindowViewModel.RemoveDevice(ups);
            }
        }

        public IManagementService Channel { get; private set; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool isConnectedToService;

        public bool IsConnectedToService
        {
            get => this.isConnectedToService;
            set => this.SetProperty(ref this.isConnectedToService, value);
        }

        public Exception ConnectToServiceException { get; set; }

        public void ConnectToService()
        {
            Task.Run(async () =>
            {
                CallbackClient callbackClient = new CallbackClient();
                InstanceContext context = new InstanceContext(callbackClient);

                try
                {
                    await Task.Delay(1000).ConfigureAwait(false);

                    DuplexChannelFactory<IManagementService> factory =
                        new DuplexChannelFactory<IManagementService>(
                            context,
                            new NetNamedPipeBinding(),
                            new EndpointAddress("net.pipe://localhost/Wingnut"));

                    this.Channel = factory.CreateChannel();

                    // Connection was successful
                    this.IsConnectedToService = true;

                    // Get the list of all UPS devices from all servers (aka every UPS device being
                    // monitored by the service).
                    List<Ups> upsList =
                        this.Channel.GetUps(null, null);

                    this.DeviceViewModels.Clear();

                    foreach (Ups ups in upsList)
                    {
                        AddDeviceToService(ups);
                    }

                    this.Channel.Register();

                    // TODO: Now switch the view to the first UPS device that is not being monitored
                }
                catch (Exception exception)
                {
                    this.ConnectToServiceException = exception;
                    this.IsConnectedToService = false;
                }
            });
        }

        public void AddDeviceToService(Ups ups)
        {
            UpsConfiguration upsConfiguration =
                App.Current.MainWindowViewModel.Channel
                    .GetUpsConfiguration(ups.Server.Name, ups.Name);

            ups.Initialize();
            UpsDeviceViewModel deviceViewModel = new UpsDeviceViewModel(ups, upsConfiguration);

            App.DispatcherInvoke(() =>
            {
                this.DeviceViewModels.Add(deviceViewModel);

                if (SelectedDevice == null)
                {
                    this.SelectedDevice = deviceViewModel;
                }
            });
        }

        private void RemoveDevice(Ups ups)
        {
            this.SelectedDevice = this.DeviceViewModels.FirstOrDefault(d => d.Device != ups);
            var viewModel = this.DeviceViewModels.FirstOrDefault(vm => vm.Device == ups);
            if (viewModel != null)
            {
                this.DeviceViewModels.Remove(viewModel);
            }
        }

        public void Dispose()
        {
        }

        public void Initialize()
        {
            var homePage = new HomePageViewModel();
            this.Pages.Add(homePage);
            homePage.IsSelected = true;

            this.Pages.Add(new StatusPageViewModel());
            this.Pages.Add(new NotificationsPageViewModel());
            this.Pages.Add(new EnergyUsagePageViewModel());
            this.Pages.Add(new SettingsPageViewModel());
        }

        //public void Initialize()
        //{
        //    var homePageSection = new HomePageNavigationViewModel(new HomePageViewModel());
        //    this.NavigationSections.Add(homePageSection);
        //    homePageSection.IsSelected = true;

        //    this.NavigationSections.Add(new UpsStatusNavigationViewModel());
        //    this.NavigationSections.Add(new UpsNotificationsNavigationViewModel());
        //    this.NavigationSections.Add(new UpsEnergyUsageNavigationViewModel());
        //    this.NavigationSections.Add(new UpsSettingsNavigationViewModel());
        //}
    }
}