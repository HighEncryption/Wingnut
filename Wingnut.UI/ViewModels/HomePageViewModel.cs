namespace Wingnut.UI.ViewModels
{
    using System;
    using System.Diagnostics;
    using System.Windows.Input;

    using Wingnut.UI.Framework;
    using Wingnut.UI.Windows;

    public class HomePageViewModel : ViewModelBase
    {
        public ICommand AddDeviceCommand { get; }

        public HomePageViewModel()
        {
            this.AddDeviceCommand = new DelegatedCommand(this.AddDevice, this.CanAddDevice);
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
                // Add the device
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
    }
}