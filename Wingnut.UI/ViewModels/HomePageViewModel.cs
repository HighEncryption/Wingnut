namespace Wingnut.UI.ViewModels
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using System.Windows.Input;

    using Wingnut.Data;
    using Wingnut.Data.Models;
    using Wingnut.Tracing;
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
                if (windowViewModel.SelectedDevice is Ups ups)
                {
                    Task<Ups> addUpsTask =
                        App.Current.MainWindowViewModel.Channel.AddUps(
                            ups.Server,
                            windowViewModel.Password.GetDecrypted(),
                            ups.Name,
                            Constants.DefaultNumPowerSupplies,
                            false,
                            false);

                    addUpsTask.Wait();

                    Ups addedUps = addUpsTask.Result;

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
}