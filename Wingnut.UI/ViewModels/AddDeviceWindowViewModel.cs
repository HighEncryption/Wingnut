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
    using Wingnut.Data.Configuration;
    using Wingnut.Data.Models;
    using Wingnut.UI.Framework;

    public class AddDeviceWindowViewModel : ViewModelBase
    {
        public event RequestCloseEventHandler RequestClose;

        public Device SelectedDevice { get; set; }

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

        public ICommand GetDevicesCommand { get; set; }

        private ObservableCollection<AddDeviceGroupViewModel> deviceGroups;

        public ObservableCollection<AddDeviceGroupViewModel> DeviceGroups =>
            this.deviceGroups ?? (this.deviceGroups = new ObservableCollection<AddDeviceGroupViewModel>());

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
                    AddDeviceGroupViewModel groupViewModel = new AddDeviceGroupViewModel(
                        "Uninterruptible power supply");

                    foreach (Ups device in devices)
                    {
                        groupViewModel.Devices.Add(
                            new AddDeviceViewModel(
                                this,
                                "\uF607",
                                device));
                    }

                    App.DispatcherInvoke(() =>
                    {
                        this.DeviceGroups.Add(groupViewModel);
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