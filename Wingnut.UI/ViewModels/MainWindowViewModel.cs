namespace Wingnut.UI.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.ServiceModel;
    using System.Threading.Tasks;
    using System.Windows.Input;

    using Wingnut.Channels;
    using Wingnut.Data.Models;
    using Wingnut.UI.Framework;
    using Wingnut.UI.Navigation;

    public class MainWindowViewModel : ViewModelBase, IDisposable
    {
        private ObservableCollection<NavigationItemViewModel> navigationItems;

        public ObservableCollection<NavigationItemViewModel> NavigationItems =>
            this.navigationItems ??
            (this.navigationItems = new ObservableCollection<NavigationItemViewModel>());

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private NavigationItemViewModel selectedNavigationItem;

        public NavigationItemViewModel SelectedNavigationItem
        {
            get => this.selectedNavigationItem;
            set => this.SetProperty(ref this.selectedNavigationItem, value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private NavigationSectionViewModel selectedNavigationSection;

        public NavigationSectionViewModel SelectedNavigationSection
        {
            get => this.selectedNavigationSection;
            set
            {
                var previousNavSection = this.selectedNavigationSection;

                if (this.SetProperty(ref this.selectedNavigationSection, value) &&
                    previousNavSection != null)
                {
                    previousNavSection.IsSelected = false;
                }
            } 
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private NavigationSectionViewModel homePageNavigationViewModel;

        public NavigationSectionViewModel HomePageNavigationViewModel
        {
            get => this.homePageNavigationViewModel;
            set => this.SetProperty(ref this.homePageNavigationViewModel, value);
        }

        private ObservableCollection<DeviceViewModel> deviceViewModels;

        public ObservableCollection<DeviceViewModel> DeviceViewModels =>
            this.deviceViewModels ?? (this.deviceViewModels = new ObservableCollection<DeviceViewModel>());


        private class CallbackClient : IManagementCallback
        {
            public void SendCallbackMessage(string message)
            {
                throw new System.NotImplementedException();
            }

            public void UpsDeviceChanged(Ups ups)
            {
                throw new NotImplementedException();
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
                    await Task.Delay(2000).ConfigureAwait(false);

                    DuplexChannelFactory<IManagementService> factory =
                        new DuplexChannelFactory<IManagementService>(
                            context,
                            new NetNamedPipeBinding(),
                            new EndpointAddress("net.pipe://localhost/Wingnut"));

                    this.Channel = factory.CreateChannel();

                    // Connection was successful
                    this.IsConnectedToService = true;

                    List<Ups> upsList =
                        await this.Channel.GetUps(null, null).ConfigureAwait(false);

                    this.DeviceViewModels.Clear();

                    foreach (Ups ups in upsList)
                    {
                        UpsDeviceViewModel deviceViewModel = new UpsDeviceViewModel(ups);

                        App.DispatcherInvoke(() =>
                        {
                            this.DeviceViewModels.Add(deviceViewModel);
                            this.NavigationItems.Add(new UpsDeviceNavigationViewModel(deviceViewModel));
                        });
                    }
                }
                catch (Exception exception)
                {
                    this.ConnectToServiceException = exception;
                    this.IsConnectedToService = false;
                }
            });
        }

        public void Dispose()
        {
        }

        public MainWindowViewModel()
        {
        }

        public void SetHomePage()
        {
            this.HomePageNavigationViewModel = new HomePageNavigationViewModel(new HomePageViewModel());
            this.HomePageNavigationViewModel.IsSelected = true;
            this.SelectedNavigationSection = this.HomePageNavigationViewModel;
        }
    }

    public abstract class DeviceViewModel : ViewModelBase
    {

    }

    public class UpsDeviceViewModel : DeviceViewModel
    {
        private readonly Ups ups;

        public UpsDeviceViewModel(Ups ups)
        {
            this.ups = ups;
        }

        public string UpsName => this.ups.Name;
    }

    public class HomePageViewModel : ViewModelBase
    {
        public ICommand AddDeviceCommand { get; }

        public HomePageViewModel()
        {
            this.AddDeviceCommand = new DelegatedCommand(this.AddDevice, this.CanAddDevice);
        }

        private void AddDevice(object obj)
        {
            throw new NotImplementedException();
        }

        private bool CanAddDevice(object obj)
        {
            return App.Current.MainWindowViewModel.IsConnectedToService;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool showAddDeviceButton;

        public bool ShowAddDeviceButton
        {
            get => this.showAddDeviceButton;
            set => this.SetProperty(ref this.showAddDeviceButton, value);
        }
    }
}