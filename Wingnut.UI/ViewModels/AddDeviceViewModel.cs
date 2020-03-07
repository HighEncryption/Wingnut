namespace Wingnut.UI.ViewModels
{
    using System.Collections.Generic;
    using System.Windows.Input;

    using Wingnut.Data.Models;
    using Wingnut.UI.Framework;

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

        private readonly Device device;

        public ICommand AddDeviceCommand { get; }

        public string DeviceName => this.device.Name;

        public string MakeAndModel =>
            $"{device.Manufacturer} {device.Model}";

        public bool IsManaged =>
            this.device.IsManaged;

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
            Device device)
        {
            this.windowViewModel = windowViewModel;
            this.Glyph = glyph;
            this.device = device;

            this.AddDeviceCommand = new DelegatedCommand(
                this.AddDeviceOnExecute,
                (o) => !this.device.IsManaged);
        }

        private void AddDeviceOnExecute(object obj)
        {
            this.windowViewModel.SelectedDevice = this.device;
            this.windowViewModel.CloseWindow(true);
        }
    }
}
