namespace Wingnut.UI.ViewModels
{
    using System;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Input;

    using Wingnut.Data;
    using Wingnut.Data.Models;
    using Wingnut.Tracing;
    using Wingnut.UI.Framework;
    using Wingnut.UI.Windows;

    public class HomePageViewModel : PageViewModel
    {
        public ICommand AddDeviceCommand { get; }

        public ICommand RemoveDeviceCommand { get; }

        private bool showStatusMessage;

        public bool ShowStatusMessage
        {
            get => this.showStatusMessage;
            set => this.SetProperty(ref this.showStatusMessage, value);
        }

        public MainWindowViewModel MainWindow
            => App.Current.MainWindowViewModel;

        public HomePageViewModel()
        {
            this.Glyph = "\uE80F";
            this.NavigationHeader = "Home";
            this.PageHeader = "Home";
            this.HeaderImage = "Resources/Graphics/ups_header.jpg";

            this.AddDeviceCommand = new DelegatedCommand(this.AddDevice, this.CanAddDevice);

            this.RemoveDeviceCommand = new DelegatedCommand(
                this.RemoveDevice,
                this.CanRemoveDevice);

            App.Current.MainWindowViewModel.DeviceViewModels.CollectionChanged +=
                this.BuildDeviceGroups;
        }

        public override bool IsEnabled => true;

        private bool CanRemoveDevice(object obj)
        {
            return true;
        }

        private void RemoveDevice(object obj)
        {
            var result = MessageBox.Show(
                "Are you sure you want to remove this device?",
                "Remove Device",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            if (result == MessageBoxResult.Yes)
            {
                App.Current.MainWindowViewModel.Channel.RemoveUps(
                    this.ActiveDevice.Device.Server.Name,
                    this.ActiveDevice.DeviceName);
            }
        }

        private void BuildDeviceGroups(object sender, NotifyCollectionChangedEventArgs e)
        {
            //var collection = sender as ObservableCollection<DeviceViewModel>;

            //if (collection == null)
            //{
            //    return;
            //}

            //App.DispatcherInvoke(() => { this.DeviceGroups.Clear(); });

            //var upsDevices = collection.OfType<UpsDeviceViewModel>().ToList();
            //if (upsDevices.Any())
            //{
            //    DeviceReferenceGroupViewModel upsReferenceGroupViewModel = new DeviceReferenceGroupViewModel(
            //        "Uninterruptible power supply");

            //    foreach (UpsDeviceViewModel upsDeviceViewModel in upsDevices)
            //    {
            //        var deviceReferenceViewModel = new DeviceReferenceViewModel(
            //            this,
            //            "\uF607",
            //            upsDeviceViewModel.Device);

            //        deviceReferenceViewModel.OnDeviceRemoved += (o, args) =>
            //        {
            //            // TODO: What to do when removal is requested?
            //        };

            //        upsReferenceGroupViewModel.Devices.Add(deviceReferenceViewModel);
            //    }

            //    App.DispatcherInvoke(() => { this.DeviceGroups.Add(upsReferenceGroupViewModel); });
            //}
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
                    App.Current.MainWindowViewModel.Channel.AddUps(
                        ups.Server,
                        windowViewModel.Password.GetDecrypted(),
                        ups.Name,
                        Constants.DefaultNumPowerSupplies,
                        false,
                        false);
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
    }
}