namespace Wingnut.UI.ViewModels
{
    using Wingnut.Data.Models;
    using Wingnut.UI.Framework;

    public abstract class DeviceViewModel : ViewModelBase
    {
        public abstract string DeviceName { get; }

        public abstract string MakeAndModel { get; }

        public abstract Device Device { get; }
    }
}