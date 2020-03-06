namespace Wingnut.UI.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;

    using Wingnut.Data;
    using Wingnut.Data.Configuration;
    using Wingnut.Data.Models;

    public class UpsDeviceViewModel : DeviceViewModel
    {
        private UpsConfiguration configuration;

        public Ups Ups { get; }

        public override Device Device => this.Ups;

        #region Status Page Properties

        private ObservableCollection<DevicePropertyViewModel> powerStatusLeftProperties;

        public ObservableCollection<DevicePropertyViewModel> PowerStatusLeftProperties =>
            this.powerStatusLeftProperties ?? (this.powerStatusLeftProperties = new ObservableCollection<DevicePropertyViewModel>());

        private ObservableCollection<DevicePropertyViewModel> powerStatusRightProperties;

        public ObservableCollection<DevicePropertyViewModel> PowerStatusRightProperties =>
            this.powerStatusRightProperties ?? (this.powerStatusRightProperties = new ObservableCollection<DevicePropertyViewModel>());

        private ObservableCollection<DevicePropertyViewModel> batteryStatusLeftProperties;

        public ObservableCollection<DevicePropertyViewModel> BatteryStatusLeftProperties =>
            this.batteryStatusLeftProperties ?? (this.batteryStatusLeftProperties = new ObservableCollection<DevicePropertyViewModel>());

        private ObservableCollection<DevicePropertyViewModel> batteryStatusRightProperties;

        public ObservableCollection<DevicePropertyViewModel> BatteryStatusRightProperties =>
            this.batteryStatusRightProperties ?? (this.batteryStatusRightProperties = new ObservableCollection<DevicePropertyViewModel>());

        private ObservableCollection<DevicePropertyViewModel> deviceInfoLeftProperties;

        public ObservableCollection<DevicePropertyViewModel> DeviceInfoLeftProperties =>
            this.deviceInfoLeftProperties ?? (this.deviceInfoLeftProperties = new ObservableCollection<DevicePropertyViewModel>());

        private ObservableCollection<DevicePropertyViewModel> deviceInfoRightProperties;

        public ObservableCollection<DevicePropertyViewModel> DeviceInfoRightProperties =>
            this.deviceInfoRightProperties ?? (this.deviceInfoRightProperties = new ObservableCollection<DevicePropertyViewModel>());

        #endregion

        #region Notification Page Properties

        private bool emailNotificationEnabled;

        public bool EmailNotificationEnabled
        {
            get => this.emailNotificationEnabled;
            set => this.SetProperty(ref this.emailNotificationEnabled, value);
        }

        private bool powerShellNotificationEnabled;

        public bool PowerShellNotificationEnabled
        {
            get => this.powerShellNotificationEnabled;
            set => this.SetProperty(ref this.powerShellNotificationEnabled, value);
        }

        #endregion

        #region Settings Page Properties

        private bool monitorOnly;

        public bool MonitorOnly
        {
            get => this.monitorOnly;
            set => this.SetProperty(ref this.monitorOnly, value);
        }

        #endregion

        public UpsDeviceViewModel(Ups ups, UpsConfiguration configuration)
        {
            this.Ups = ups;
            this.configuration = configuration;

            this.EmailNotificationEnabled = configuration.EnableEmailNotification;
            this.PowerShellNotificationEnabled = configuration.EnablePowerShellNotification;

            this.UpdateStatusDisplayString();
            this.UpdateStatusSeverity();
            this.UpdateEstimatedRuntimeDisplayString();
            this.UpdateEstimatedRuntimeSeverity();

            this.Ups.PropertyChanged += this.UpsOnPropertyChanged;

            this.CreateDevicePropertyViewModels();
        }

        private void CreateDevicePropertyViewModels()
        {
            //
            // Power Status Left Column
            //
            this.PowerStatusLeftProperties.Add(
                new DevicePropertyViewModel(
                    "Input voltage:",
                    this.Ups,
                    nameof(this.Ups.InputVoltage),
                    () => FormatHelpers.FormatVoltage(this.Ups.InputVoltage)));

            this.PowerStatusLeftProperties.Add(
                new DevicePropertyViewModel(
                    "Input frequency:",
                    this.Ups,
                    nameof(this.Ups.InputFrequency),
                    () => FormatHelpers.FormatFrequency(this.Ups.InputFrequency)));

            this.PowerStatusLeftProperties.Add(
                new DevicePropertyViewModel(
                    "Input sensitivity:",
                    this.Ups,
                    nameof(this.Ups.InputSensitivity),
                    () => FormatHelpers.FormatEnum(this.Ups.InputSensitivity)));

            //
            // Power Status Right Column
            //
            this.PowerStatusRightProperties.Add(
                new DevicePropertyViewModel(
                    "Output voltage:",
                    this.Ups,
                    nameof(this.Ups.OutputVoltage),
                    () => FormatHelpers.FormatVoltage(this.Ups.OutputVoltage)));

            this.PowerStatusRightProperties.Add(
                new DevicePropertyViewModel(
                    "Output frequency:",
                    this.Ups,
                    nameof(this.Ups.OutputFrequency),
                    () => FormatHelpers.FormatFrequency(this.Ups.OutputFrequency)));

            //
            // Battery Status Left Column
            //
            this.BatteryStatusLeftProperties.Add(
                new DevicePropertyViewModel(
                    "Battery charge:",
                    this.Ups,
                    nameof(this.Ups.BatteryCharge),
                    () => FormatHelpers.FormatPercentage(this.Ups.BatteryCharge)));

            this.BatteryStatusLeftProperties.Add(
                new DevicePropertyViewModel(
                    "Battery current:",
                    this.Ups,
                    nameof(this.Ups.BatteryCurrent),
                    () => FormatHelpers.FormatCurrent(this.Ups.BatteryCurrent)));

            this.BatteryStatusLeftProperties.Add(
                new DevicePropertyViewModel(
                    "Battery replacement:",
                    this.Ups,
                    nameof(this.Ups.BatteryLastReplacement),
                    () => FormatHelpers.FormatDate(this.Ups.BatteryLastReplacement, "d")));

            //
            // Battery Status Left Column
            //
            this.BatteryStatusRightProperties.Add(
                new DevicePropertyViewModel(
                    "Battery runtime:",
                    this.Ups,
                    nameof(this.Ups.BatteryRuntime),
                    () => FormatHelpers.FormatMinutes(this.Ups.BatteryRuntime)));

            this.BatteryStatusRightProperties.Add(
                new DevicePropertyViewModel(
                    "Battery voltage:",
                    this.Ups,
                    nameof(this.Ups.BatteryVoltage),
                    () => FormatHelpers.FormatVoltage(this.Ups.BatteryVoltage)));

            //
            // Device Information Left Column
            //
            this.DeviceInfoLeftProperties.Add(
                new DevicePropertyViewModel(
                    "Manufacturer:",
                    this.Ups,
                    nameof(this.Ups.Manufacturer),
                    () => this.Ups.Manufacturer));

            this.DeviceInfoLeftProperties.Add(
                new DevicePropertyViewModel(
                    "Model:",
                    this.Ups,
                    nameof(this.Ups.Model),
                    () => this.Ups.Model));

            this.DeviceInfoLeftProperties.Add(
                new DevicePropertyViewModel(
                    "Device temperature:",
                    this.Ups,
                    nameof(this.Ups.Temperature),
                    () => FormatHelpers.FormatTemperature(this.Ups.Temperature, true)));

            this.DeviceInfoLeftProperties.Add(
                new DevicePropertyViewModel(
                    "Firmware:",
                    this.Ups,
                    nameof(this.Ups.Firmware),
                    () => this.Ups.Firmware));

            this.DeviceInfoLeftProperties.Add(
                new DevicePropertyViewModel(
                    "Driver name:",
                    this.Ups,
                    nameof(this.Ups.DriverName),
                    () => this.Ups.DriverName));

            //
            // Device Information Right Column
            //
            //this.DeviceInfoLeftProperties.Add(
            //    new DevicePropertyViewModel(
            //        "Device host:",
            //        this.Ups,
            //        nameof(this.Ups.Server.Name),
            //        () => this.Ups.Server.Name));

            this.DeviceInfoRightProperties.Add(
                new DevicePropertyViewModel(
                    "Device type:",
                    this.Ups,
                    nameof(this.Ups.DeviceType),
                    () => this.Ups.DeviceType.ToString()));

            this.DeviceInfoRightProperties.Add(
                new DevicePropertyViewModel(
                    "Serial:",
                    this.Ups,
                    nameof(this.Ups.Serial),
                    () => this.Ups.Serial));

            this.DeviceInfoRightProperties.Add(
                new DevicePropertyViewModel(
                    "Manufacture date:",
                    this.Ups,
                    nameof(this.Ups.ManufactureDate),
                    () => FormatHelpers.FormatDate(this.Ups.ManufactureDate, "d")));

            this.DeviceInfoRightProperties.Add(
                new DevicePropertyViewModel(
                    "Driver version:",
                    this.Ups,
                    nameof(this.Ups.DriverName),
                    () => this.Ups.DriverName));
        }



        private void UpsOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Ups.BatteryRuntime))
            {
                this.UpdateEstimatedRuntimeDisplayString();
                this.UpdateEstimatedRuntimeSeverity();
            }
            else if (e.PropertyName == nameof(Ups.BatteryRuntimeLow))
            {
                this.UpdateEstimatedRuntimeSeverity();
            }
            else if (e.PropertyName == nameof(Ups.Status))
            {
                this.UpdateStatusDisplayString();
                this.UpdateStatusSeverity();
            }
        }

        private void UpdateStatusDisplayString()
        {
            this.StatusDisplayString = string.Join(", ", this.Ups.Status.GetStatusDisplayName());
        }

        private string statusDisplayString;

        public string StatusDisplayString
        {
            get => this.statusDisplayString;
            set => this.SetProperty(ref this.statusDisplayString, value);
        }

        private void UpdateStatusSeverity()
        {
            this.StatusSeverity = Constants.Device.GetStatusSeverity(this.Ups.Status);
        }

        private DeviceSeverityType statusSeverity;

        public DeviceSeverityType StatusSeverity
        {
            get => this.statusSeverity;
            set => this.SetProperty(ref this.statusSeverity, value);
        }

        private void UpdateEstimatedRuntimeDisplayString()
        {
            this.EstimatedRuntimeDisplayString =
                this.Ups.BatteryRuntime == null
                    ? "Unknown"
                    : $"{this.Ups.BatteryRuntime.Value.TotalMinutes} minutes";
        }

        private string estimatedRuntimeDisplayString;

        public string EstimatedRuntimeDisplayString
        {
            get => this.estimatedRuntimeDisplayString;
            set => this.SetProperty(ref this.estimatedRuntimeDisplayString, value);
        }

        private void UpdateEstimatedRuntimeSeverity()
        {
            if (this.Ups.BatteryRuntimeLow == null ||
                this.Ups.BatteryRuntime == null)
            {
                this.EstimatedRuntimeSeverity = DeviceSeverityType.OK;
                return;
            }

            TimeSpan threshold = TimeSpan.FromSeconds(
                this.Ups.BatteryRuntimeLow.Value.TotalSeconds * 2);

            if (this.Ups.BatteryRuntime.Value > threshold)
            {
                this.EstimatedRuntimeSeverity = DeviceSeverityType.OK;
            }
            else if (this.Ups.BatteryRuntime.Value > this.Ups.BatteryRuntimeLow.Value)
            {
                this.EstimatedRuntimeSeverity = DeviceSeverityType.Warning;
            }
            else
            {
                this.EstimatedRuntimeSeverity = DeviceSeverityType.Error;
            }
        }

        private DeviceSeverityType estimatedRuntimeSeverity;

        public DeviceSeverityType EstimatedRuntimeSeverity
        {
            get => this.estimatedRuntimeSeverity;
            set => this.SetProperty(ref this.estimatedRuntimeSeverity, value);
        }

        public override string DeviceName => this.Ups.Name;

        public override string MakeAndModel => $"{this.Ups.Manufacturer} {this.Ups.Model}";
    }
}