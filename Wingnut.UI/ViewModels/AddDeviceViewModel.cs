namespace Wingnut.UI.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Input;

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

        public DeviceViewModel Device { get; }

        public ICommand AddDeviceCommand { get; }

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
            DeviceViewModel deviceViewModel)
        {
            this.windowViewModel = windowViewModel;
            this.Glyph = glyph;
            this.Device = deviceViewModel;

            this.AddDeviceCommand = new DelegatedCommand(this.AddDeviceOnExecute);
        }

        private void AddDeviceOnExecute(object obj)
        {
            this.windowViewModel.SelectedDevice = this.Device.Device;
            this.windowViewModel.CloseWindow(true);
        }
    }

    public delegate void RequestCloseEventHandler(object sender, RequestCloseEventArgs e);

    public class RequestCloseEventArgs : EventArgs
    {
        public RequestCloseEventArgs()
        {
        }

        public RequestCloseEventArgs(bool dialogResult)
        {
            this.DialogResult = dialogResult;
        }

        public bool? DialogResult { get; set; }
    }

}
