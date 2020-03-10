namespace Wingnut.UI.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Input;

    using Wingnut.Data.Models;
    using Wingnut.UI.Framework;

    public class DeviceReferenceGroupViewModel : ViewModelBase
    {
        public List<DeviceReferenceViewModel> Devices { get; }

        public string Header { get; }

        public DeviceReferenceGroupViewModel(string header)
        {
            this.Devices = new List<DeviceReferenceViewModel>();
            this.Header = header;
        }
    }

    public interface IDeviceReferenceContainer
    {
        DeviceReferenceViewModel SelectedDevice { get; set; }
    }

    public class DeviceReferenceViewModel : ViewModelBase
    {
        private readonly IDeviceReferenceContainer container;

        public Device Device { get; }

        public ICommand AddDeviceCommand { get; }

        public ICommand RemoveDeviceCommand { get; }

        public event EventHandler<EventArgs> OnDeviceAdded;

        public event EventHandler<EventArgs> OnDeviceRemoved;

        public string DeviceName => this.Device.Name;

        public string MakeAndModel =>
            $"{Device.Manufacturer} {Device.Model}";

        public bool IsManaged =>
            this.Device.IsManaged;

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
            set
            {
                if (this.SetProperty(ref this.isSelected, value) && value)
                {
                    this.container.SelectedDevice = this;
                }
            }
        }

        public DeviceReferenceViewModel(
            IDeviceReferenceContainer container,
            string glyph, 
            Device device)
        {
            this.container = container;
            this.Glyph = glyph;
            this.Device = device;

            this.AddDeviceCommand = new DelegatedCommand(
                this.AddDeviceOnExecute,
                o => !this.Device.IsManaged);

            this.RemoveDeviceCommand = new DelegatedCommand(
                this.RemoveDeviceOnExecute,
                o => !this.Device.IsManaged);
        }

        private void AddDeviceOnExecute(object obj)
        {
            this.OnDeviceAdded?.Invoke(this, new EventArgs());
        }

        private void RemoveDeviceOnExecute(object obj)
        {
            this.OnDeviceRemoved?.Invoke(this, new EventArgs());
        }
    }
}
