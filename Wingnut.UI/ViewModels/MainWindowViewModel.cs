namespace Wingnut.UI.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.ServiceModel;
    using System.Threading.Tasks;

    using Wingnut.Channels;
    using Wingnut.Data.Models;
    using Wingnut.UI.Framework;
    using Wingnut.UI.Navigation;

    public class MainWindowViewModel : ViewModelBase, IDisposable
    {
        private ObservableCollection<NavigationSectionViewModel> navigationSections;

        public ObservableCollection<NavigationSectionViewModel> NavigationSections =>
            this.navigationSections ?? (this.navigationSections = new ObservableCollection<NavigationSectionViewModel>());

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

        private ObservableCollection<DeviceViewModel> deviceViewModels;

        public ObservableCollection<DeviceViewModel> DeviceViewModels =>
            this.deviceViewModels ?? (this.deviceViewModels = new ObservableCollection<DeviceViewModel>());

        private class CallbackClient : IManagementCallback
        {
            public void UpsDeviceChanged(Ups ups)
            {
                ups.Initialize();

                foreach (UpsDeviceViewModel viewModel in App.Current.MainWindowViewModel.DeviceViewModels.OfType<UpsDeviceViewModel>())
                {
                    if (viewModel.Ups.QualifiedName == ups.QualifiedName)
                    {
                        //viewModel.Update(ups);
                        viewModel.Ups.UpdateVariables(ups);
                        return;
                    }
                }
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
                        AddDeviceToService(ups);
                    }

                    this.Channel.Register();
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
            ups.Initialize();
            UpsDeviceViewModel deviceViewModel = new UpsDeviceViewModel(ups);

            App.DispatcherInvoke(() =>
            {
                this.DeviceViewModels.Add(deviceViewModel);
                this.NavigationSections.Add(
                    new UpsDeviceNavigationGroupViewModel(deviceViewModel));
            });
        }

        public void Dispose()
        {
        }

        public void Initialize()
        {
            var homePageSection = new HomePageNavigationViewModel(new HomePageViewModel());
            this.NavigationSections.Add(homePageSection);
            homePageSection.IsSelected = true;
        }
    }

    public abstract class DeviceViewModel : ViewModelBase
    {
        public abstract string DeviceName { get; }

        public abstract string MakeAndModel { get; }

        public abstract Device Device { get; }
    }
}