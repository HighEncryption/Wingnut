namespace Wingnut.UI.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows.Input;

    using Wingnut.Data;
    using Wingnut.Data.Models;
    using Wingnut.Tracing;
    using Wingnut.UI.Framework;
    using Wingnut.UI.Windows;

    public class HomePageViewModel : ViewModelBase, IDeviceReferenceContainer
    {
        public ICommand AddDeviceCommand { get; }

        public HomePageViewModel()
        {
            this.AddDeviceCommand = new DelegatedCommand(this.AddDevice, this.CanAddDevice);

            App.Current.MainWindowViewModel.DeviceViewModels.CollectionChanged +=
                this.BuildDeviceGroups;
        }

        private void BuildDeviceGroups(object sender, NotifyCollectionChangedEventArgs e)
        {
            var collection = sender as ObservableCollection<DeviceViewModel>;

            if (collection == null)
            {
                return;
            }

            App.DispatcherInvoke(() => { this.DeviceGroups.Clear(); });

            var upsDevices = collection.OfType<UpsDeviceViewModel>().ToList();
            if (upsDevices.Any())
            {
                DeviceReferenceGroupViewModel upsReferenceGroupViewModel = new DeviceReferenceGroupViewModel(
                    "Uninterruptible power supply");

                foreach (UpsDeviceViewModel upsDeviceViewModel in upsDevices)
                {
                    var deviceReferenceViewModel = new DeviceReferenceViewModel(
                        this,
                        "\uF607",
                        upsDeviceViewModel.Device);

                    deviceReferenceViewModel.OnDeviceRemoved += (o, args) =>
                    {
                        // TODO: What to do when removal is requested?
                    };

                    upsReferenceGroupViewModel.Devices.Add(deviceReferenceViewModel);
                }

                App.DispatcherInvoke(() => { this.DeviceGroups.Add(upsReferenceGroupViewModel); });
            }
        }

        private void AddDevice(object obj)
        {
            AddDeviceWindowViewModel windowViewModel = new AddDeviceWindowViewModel();
            AddDeviceWindow window = new AddDeviceWindow
            {
                DataContext = windowViewModel
            };

            bool? result = window.ShowDialog();
            if (result == true)
            {
                AddDeviceToService(windowViewModel);
            }
        }

        private ObservableCollection<DeviceReferenceGroupViewModel> deviceGroups;

        public ObservableCollection<DeviceReferenceGroupViewModel> DeviceGroups =>
            this.deviceGroups ?? (this.deviceGroups = new ObservableCollection<DeviceReferenceGroupViewModel>());


        private bool CanAddDevice(object obj)
        {
            return true;
            //return App.Current.MainWindowViewModel.IsConnectedToService;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool showAddDeviceButton;

        public bool ShowAddDeviceButton
        {
            get => this.showAddDeviceButton;
            set => this.SetProperty(ref this.showAddDeviceButton, value);
        }

        private void AddDeviceToService(AddDeviceWindowViewModel windowViewModel)
        {
            try
            {
                if (windowViewModel.DeviceToAdd is Ups ups)
                {
                    Ups addedUps =
                        App.Current.MainWindowViewModel.Channel.AddUps(
                            ups.Server,
                            windowViewModel.Password.GetDecrypted(),
                            ups.Name,
                            Constants.DefaultNumPowerSupplies,
                            false,
                            false);

                    App.Current.MainWindowViewModel.AddDeviceToService(addedUps);
                }
                else
                {
                    throw new Exception("Unknown device type");
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed to add UPS device. The error was: {0}", e.Message);
                Logger.Debug("Failed to add UPS device. Exception: {0}", e);
            }
        }

        private DeviceReferenceViewModel selectedDevice;

        public DeviceReferenceViewModel SelectedDevice
        {
            get => this.selectedDevice;
            set
            {
                DeviceReferenceViewModel previousValue = this.selectedDevice;
                if (this.SetProperty(ref this.selectedDevice, value) &&
                    previousValue != null &&
                    previousValue != value)
                {
                    previousValue.IsSelected = false;
                }
            }
        }
    }
}