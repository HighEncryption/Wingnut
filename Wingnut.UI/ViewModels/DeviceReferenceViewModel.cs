namespace Wingnut.UI.ViewModels
{
    using Wingnut.Data.Models;
    using Wingnut.UI.Framework;

    public class DeviceReferenceViewModel : ViewModelBase
    {
        public Device Device { get; }

        public string DeviceName => this.Device.Name;

        public string DeviceNameWithManagedText =>
            string.Format("{0}{1}",
                this.DeviceName,
                this.Device.IsManaged ? " (Already added)" : string.Empty);

        public string MakeAndModel =>
            $"{Device.Manufacturer} {Device.Model}";

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

        private bool isEnabled;

        public bool IsEnabled
        {
            get => this.isEnabled;
            set => this.SetProperty(ref this.isEnabled, value);
        }

        public DeviceReferenceViewModel(
            string glyph, 
            Device device)
        {
            this.Glyph = glyph;
            this.Device = device;
        }
    }
}
