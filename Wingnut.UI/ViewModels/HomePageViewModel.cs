namespace Wingnut.UI.ViewModels
{
    using System;
    using System.Diagnostics;
    using System.Windows.Input;

    using Wingnut.UI.Framework;

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