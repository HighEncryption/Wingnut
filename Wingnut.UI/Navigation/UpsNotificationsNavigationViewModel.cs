namespace Wingnut.UI.Navigation
{
    using System.Windows.Input;

    using Wingnut.UI.Framework;
    using Wingnut.UI.ViewModels;

    public class UpsNotificationsNavigationViewModel : NavigationSectionViewModel
    {
        public UpsDeviceViewModel DeviceViewModel { get; }

        public ICommand ConfigureEmailSettings { get; }

        public ICommand ConfigurePowerShellSettings { get; }

        public UpsNotificationsNavigationViewModel(UpsDeviceViewModel deviceViewModel)
        {
            this.DeviceViewModel = deviceViewModel;
            this.NavigationHeader = "Notifications";
            this.PageHeader = "Notifications";
            this.Glyph = "\uEC42";

            this.ConfigureEmailSettings = new DelegatedCommand(
                this.ConfigureEmailSettingsOnExecute,
                o => this.DeviceViewModel.EmailNotificationEnabled);

            this.ConfigurePowerShellSettings = new DelegatedCommand(
                this.ConfigurePowerShellSettingsOnExecute,
                o => this.DeviceViewModel.PowerShellNotificationEnabled);
        }

        private void ConfigureEmailSettingsOnExecute(object obj)
        {
            
        }

        private void ConfigurePowerShellSettingsOnExecute(object obj)
        {
            throw new System.NotImplementedException();
        }
    }
}