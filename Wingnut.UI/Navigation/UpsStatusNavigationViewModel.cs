namespace Wingnut.UI.Navigation
{
    using Wingnut.UI.ViewModels;

    public class UpsStatusNavigationViewModel : NavigationSectionViewModel
    {
        public UpsDeviceViewModel Device { get; }

        public UpsStatusNavigationViewModel(UpsDeviceViewModel device)
        {
            this.Device = device;
            this.NavigationHeader = "Status";
            this.PageHeader = "Status";
            this.Glyph = "\uEC4A";
        }
    }
}