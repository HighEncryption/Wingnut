namespace Wingnut.UI.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Security;
    using System.Threading.Tasks;
    using System.Windows.Input;

    using Wingnut.Data;
    using Wingnut.Data.Models;
    using Wingnut.UI.Framework;

    public class AddDeviceWindowViewModel : ViewModelBase, IDeviceReferenceContainer
    {
        public event RequestCloseEventHandler RequestClose;

        public Device DeviceToAdd { get; set; }

        private string serverAddress;

        public string ServerAddress
        {
            get => this.serverAddress;
            set => this.SetProperty(ref this.serverAddress, value);
        }

        private string serverPort;

        public string ServerPort
        {
            get => this.serverPort;
            set => this.SetProperty(ref this.serverPort, value);
        }

        private string username;

        public string Username
        {
            get => this.username;
            set => this.SetProperty(ref this.username, value);
        }

        private SecureString password;

        public SecureString Password
        {
            get => this.password;
            set => this.SetProperty(ref this.password, value);
        }

        private string connectionError;

        public string ConnectionError
        {
            get => this.connectionError;
            set => this.SetProperty(ref this.connectionError, value);
        }

        private bool isConnecting;

        public bool IsConnecting
        {
            get => this.isConnecting;
            set => this.SetProperty(ref this.isConnecting, value);
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

        public ICommand GetDevicesCommand { get; set; }

        private ObservableCollection<DeviceReferenceGroupViewModel> deviceGroups;

        public ObservableCollection<DeviceReferenceGroupViewModel> DeviceGroups =>
            this.deviceGroups ?? (this.deviceGroups = new ObservableCollection<DeviceReferenceGroupViewModel>());

        public AddDeviceWindowViewModel()
        {
            this.ServerPort = "3493";

            this.GetDevicesCommand = new DelegatedCommand(
                this.GetDevicesOnExecute,
                this.GetDevicesCanExecute);
        }

        private bool GetDevicesCanExecute(object obj)
        {
            return !string.IsNullOrWhiteSpace(this.ServerAddress) &&
                   !string.IsNullOrWhiteSpace(this.ServerPort) &&
                   !string.IsNullOrWhiteSpace(this.Username) &&
                   this.Password != null &&
                   this.Password.Length > 0;
        }

        private void GetDevicesOnExecute(object obj)
        {
            Task.Run(() => this.GetDevices());
        }

        public void CloseWindow(bool dialogResult)
        {
            this.RequestClose?.Invoke(this, new RequestCloseEventArgs(dialogResult));
        }

        private void GetDevices()
        {
            this.ConnectionError = null;
            this.IsConnecting = true;

            try
            {
                Server server = new Server()
                {
                    Address = this.ServerAddress,
                    Port = 3493,
                    Username = this.Username,
                };

                List<Ups> devices =
                    App.Current.MainWindowViewModel.Channel
                        .GetUpsFromServer(server, this.Password.GetDecrypted(), null);

                App.DispatcherInvoke(() =>
                {
                    this.DeviceGroups.Clear();
                });

                if (devices.Any())
                {
                    DeviceReferenceGroupViewModel referenceGroupViewModel = new DeviceReferenceGroupViewModel(
                        "Uninterruptible power supply");

                    foreach (Ups device in devices)
                    {
                        var deviceReference = new DeviceReferenceViewModel(
                            this,
                            "\uF607",
                            device);

                        deviceReference.OnDeviceAdded += (sender, args) =>
                        {
                            this.DeviceToAdd = ((DeviceReferenceViewModel) sender).Device;
                            this.CloseWindow(true);
                        };

                        referenceGroupViewModel.Devices.Add(deviceReference);
                    }

                    App.DispatcherInvoke(() =>
                    {
                        this.DeviceGroups.Add(referenceGroupViewModel);
                    });
                }
            }
            catch (Exception exception)
            {
                this.ConnectionError = exception.Message;
            }
            finally
            {
                this.IsConnecting = false;
            }
        }
    }
}