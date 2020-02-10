namespace Wingnut.UI.ViewModels
{
    using Wingnut.Data.Models;

    public class UpsDeviceViewModel : DeviceViewModel
    {
        private readonly Ups ups;

        public UpsDeviceViewModel(Ups ups)
        {
            this.ups = ups;
        }

        public string UpsName => this.ups.Name;
    }
}