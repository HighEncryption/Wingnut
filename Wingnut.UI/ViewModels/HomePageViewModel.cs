namespace Wingnut.UI.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Input;

    using Wingnut.Data;
    using Wingnut.Data.Models;
    using Wingnut.Tracing;
    using Wingnut.UI.Framework;
    using Wingnut.UI.Windows;

    public abstract class PageViewModel : ViewModelBase
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string pageHeader;

        public string PageHeader
        {
            get => this.pageHeader;
            set => this.SetProperty(ref this.pageHeader, value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string navigationHeader;

        public string NavigationHeader
        {
            get => this.navigationHeader;
            set => this.SetProperty(ref this.navigationHeader, value);
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
                    App.Current.MainWindowViewModel.SelectedPage = this;
                }
            }
        }

        protected PageViewModel()
        {
            App.Current.MainWindowViewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(MainWindowViewModel.SelectedDevice))
                {
                    this.RaisePropertyChanged(nameof(this.ActiveDevice));
                }
            };
        }

        public UpsDeviceViewModel ActiveDevice =>
            App.Current.MainWindowViewModel.SelectedDevice as UpsDeviceViewModel;
    }

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

        private bool? showNoDevicesMonitored;

        public bool? ShowNoDevicesMonitored
        {
            get => this.showNoDevicesMonitored;
            set => this.SetProperty(ref this.showNoDevicesMonitored, value);
        }

        public MainWindowViewModel MainWindow
            => App.Current.MainWindowViewModel;

        public HomePageViewModel()
        {
            this.Glyph = "\uE80F";
            this.NavigationHeader = "Home";
            this.PageHeader = "Home";

            this.AddDeviceCommand = new DelegatedCommand(this.AddDevice, this.CanAddDevice);

            this.RemoveDeviceCommand = new DelegatedCommand(
                this.RemoveDevice,
                this.CanRemoveDevice);

            App.Current.MainWindowViewModel.DeviceViewModels.CollectionChanged +=
                this.BuildDeviceGroups;
        }

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

        //private ObservableCollection<DeviceReferenceGroupViewModel> deviceGroups;

        //public ObservableCollection<DeviceReferenceGroupViewModel> DeviceGroups =>
        //    this.deviceGroups ?? (this.deviceGroups = new ObservableCollection<DeviceReferenceGroupViewModel>());


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
    }

    public class StatusPageViewModel : PageViewModel
    {
        public StatusPageViewModel()
        {
            this.NavigationHeader = "Status";
            this.PageHeader = "Status";
            this.Glyph = "\uEC4A";
        }
    }

    public class NotificationsPageViewModel : PageViewModel
    {
        public NotificationsPageViewModel()
        {
            this.NavigationHeader = "Notifications";
            this.PageHeader = "Notifications";
            this.Glyph = "\uEC42";
        }
    }


    public class EnergyUsagePageViewModel : PageViewModel
    {
        public EnergyUsagePageViewModel()
        {
            this.NavigationHeader = "Energy Usage";
            this.PageHeader = "Energy Usage";
            this.Glyph = "\uE9D2";
        }
    }

    public class SettingsPageViewModel : PageViewModel
    {
        public SettingsPageViewModel()
        {
            this.NavigationHeader = "Settings";
            this.PageHeader = "Settings";
            this.Glyph = "\uE713";
        }
    }
}