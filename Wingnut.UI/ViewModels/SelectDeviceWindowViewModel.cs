namespace Wingnut.UI.ViewModels
{
    using System.Collections.Generic;
    using System.Windows.Input;

    using Wingnut.UI.Framework;

    public class SelectDeviceWindowViewModel : ViewModelBase
    {
        public event RequestCloseEventHandler RequestClose;

        public ICommand SelectDeviceCommand { get; set; }

        public ICommand CancelCommand { get; set; }

        public List<DeviceReferenceViewModel> DeviceReferences { get; }

        private DeviceReferenceViewModel selectedDeviceReference;

        public DeviceReferenceViewModel SelectedDeviceReference
        {
            get => this.selectedDeviceReference;
            set => this.SetProperty(ref this.selectedDeviceReference, value);
        }

        public SelectDeviceWindowViewModel()
        {
            this.DeviceReferences = new List<DeviceReferenceViewModel>();

            this.SelectDeviceCommand = new DelegatedCommand(
                this.SelectDeviceOnExecute,
                this.SelectDeviceCanExecute);

            this.CancelCommand = new DelegatedCommand(
                o => this.CloseWindow(false));
        }

        private bool SelectDeviceCanExecute(object obj)
        {
            return this.SelectedDeviceReference != null && 
                   this.SelectedDeviceReference.Device.IsManaged == false;
        }

        private void SelectDeviceOnExecute(object obj)
        {
            this.CloseWindow(true);
        }

        public void CloseWindow(bool dialogResult)
        {
            this.RequestClose?.Invoke(this, new RequestCloseEventArgs(dialogResult));
        }
    }
}