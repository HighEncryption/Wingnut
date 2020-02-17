namespace Wingnut.UI.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Security;
    using System.ServiceModel;
    using System.Windows.Input;

    using Wingnut.Channels;
    using Wingnut.Data;
    using Wingnut.Data.Models;
    using Wingnut.UI.Framework;

    public class AddDeviceWindowViewModel : ViewModelBase
    {
        private class CallbackClient : IManagementCallback
        {
            public void UpsDeviceChanged(Ups ups)
            {
            }
        }

        public event RequestCloseEventHandler RequestClose;


        private IManagementService Channel { get; set; }

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

        public ICommand GetDevicesCommand { get; set; }

        private ObservableCollection<AddDeviceGroupViewModel> deviceGroups;

        public ObservableCollection<AddDeviceGroupViewModel> DeviceGroups =>
            this.deviceGroups ?? (this.deviceGroups = new ObservableCollection<AddDeviceGroupViewModel>());

        public AddDeviceWindowViewModel()
        {
            this.ServerPort = "3493";

            this.GetDevicesCommand = new DelegatedCommand(this.GetDeviceOnExecute, this.GetDevicesCanExecute);
        }

        private bool GetDevicesCanExecute(object obj)
        {
            return true;
        }

        public void CloseWindow(bool dialogResult)
        {
            this.RequestClose?.Invoke(this, new RequestCloseEventArgs(dialogResult));
        }

        private void GetDeviceOnExecute(object obj)
        {
            // TODO: Move to a background task!!
            CallbackClient callbackClient = new CallbackClient();
            InstanceContext context = new InstanceContext(callbackClient);

            try
            {
                DuplexChannelFactory<IManagementService> factory =
                    new DuplexChannelFactory<IManagementService>(
                        context,
                        new NetNamedPipeBinding(),
                        new EndpointAddress("net.pipe://localhost/Wingnut"));

                this.Channel = factory.CreateChannel();

                Server server = new Server()
                {
                    Address = this.ServerAddress,
                    Port = 3493,
                    Username = this.Username,
                };

                List<Ups> devices = this.Channel.GetUpsFromServer(server, this.Password.GetDecrypted(), null).Result;

                this.DeviceGroups.Clear();

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
                                new UpsDeviceViewModel(device))
                            );
                    }

                    this.DeviceGroups.Add(groupViewModel);
                }
            }
            catch (Exception exception)
            {
            }
        }
    }

    public class AddDeviceGroupViewModel : ViewModelBase
    {
        public List<AddDeviceViewModel> Devices { get; }

        public string Header { get; }

        public AddDeviceGroupViewModel(string header)
        {
            this.Devices = new List<AddDeviceViewModel>();
            this.Header = header;
        }
    }

    public class AddDeviceViewModel : ViewModelBase
    {
        private readonly AddDeviceWindowViewModel windowViewModel;

        public DeviceViewModel Device { get; }

        public ICommand AddDeviceCommand { get; }

        private string glyph;

        public string Glyph
        {
            get => this.glyph;
            set => this.SetProperty(ref this.glyph, value);
        }

        private bool isSelected;

        public bool IsSelected
        {
            get => this.isSelected;
            set => this.SetProperty(ref this.isSelected, value);
        }

        public AddDeviceViewModel(
            AddDeviceWindowViewModel windowViewModel, 
            string glyph, 
            DeviceViewModel deviceViewModel)
        {
            this.windowViewModel = windowViewModel;
            this.Glyph = glyph;
            this.Device = deviceViewModel;

            this.AddDeviceCommand = new DelegatedCommand(this.AddDeviceOnExecute);
        }

        private void AddDeviceOnExecute(object obj)
        {
            this.windowViewModel.SelectedDevice = this.Device.Device;
            this.windowViewModel.CloseWindow(true);
        }
    }

    public delegate void RequestCloseEventHandler(object sender, RequestCloseEventArgs e);

    public class RequestCloseEventArgs : EventArgs
    {
        public RequestCloseEventArgs()
        {
        }

        public RequestCloseEventArgs(bool dialogResult)
        {
            this.DialogResult = dialogResult;
        }

        public bool? DialogResult { get; set; }
    }

}
