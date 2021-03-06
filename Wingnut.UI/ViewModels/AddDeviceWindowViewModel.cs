﻿namespace Wingnut.UI.ViewModels
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
    using Wingnut.UI.Windows;

    public class AddDeviceWindowViewModel : ViewModelBase//, IDeviceReferenceContainer
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

        //private bool showDeviceReferences;

        //public bool ShowDeviceReferences
        //{
        //    get => this.showDeviceReferences;
        //    set => this.SetProperty(ref this.showDeviceReferences, value);
        //}

        private bool devicesFound;

        public bool DevicesFound
        {
            get => this.devicesFound;
            set => this.SetProperty(ref this.devicesFound, value);
        }

        private bool noDevicesFound;

        public bool NoDevicesFound
        {
            get => this.noDevicesFound;
            set => this.SetProperty(ref this.noDevicesFound, value);
        }

        public ICommand GetDevicesCommand { get; set; }

        public ICommand CancelCommand { get; set; }

        private ObservableCollection<DeviceReferenceViewModel> deviceReferences;

        public ObservableCollection<DeviceReferenceViewModel> DeviceReferences =>
            this.deviceReferences ?? (this.deviceReferences = new ObservableCollection<DeviceReferenceViewModel>());

        public AddDeviceWindowViewModel()
        {
            this.ServerPort = "3493";

            this.GetDevicesCommand = new DelegatedCommand(
                this.GetDevicesOnExecute,
                this.GetDevicesCanExecute);

            this.CancelCommand = new DelegatedCommand(
                (o) => { this.CloseWindow(false); });
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
                    App.Current.MainWindowViewModel.Channel.GetUpsFromServer(
                            server, 
                            this.Password.GetDecrypted(), 
                            null);

                App.DispatcherInvoke(() =>
                {
                    this.DeviceReferences.Clear();
                });

                if (devices.Any())
                {
                    foreach (Ups device in devices)
                    {
                        var deviceReference = new DeviceReferenceViewModel(
                                "\uF607",
                                device);

                        deviceReference.IsEnabled = device.IsManaged == false;

                        App.DispatcherInvoke(() =>
                        {
                            this.DeviceReferences.Add(deviceReference);
                        });
                    }
                }

                App.DispatcherInvoke(() =>
                {
                    this.DevicesFound = DeviceReferences.Any();
                    this.NoDevicesFound = !DeviceReferences.Any();

                    if (DeviceReferences.Any())
                    {
                        var selectDeviceWindowViewModel = new SelectDeviceWindowViewModel();
                        selectDeviceWindowViewModel.DeviceReferences.AddRange(this.DeviceReferences);
                        SelectDeviceWindow selectDeviceWindow = new SelectDeviceWindow
                        {
                            DataContext = selectDeviceWindowViewModel
                        };

                        var dialogResult = selectDeviceWindow.ShowDialog();
                        if (dialogResult == true)
                        {
                            this.DeviceToAdd = selectDeviceWindowViewModel.SelectedDeviceReference.Device;
                            this.CloseWindow(true);
                        }
                    }
                });
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