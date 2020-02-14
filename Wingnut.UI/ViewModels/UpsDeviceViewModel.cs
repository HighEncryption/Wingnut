namespace Wingnut.UI.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;

    using Wingnut.Data;
    using Wingnut.Data.Models;
    using Wingnut.UI.Framework;

    public class UpsDeviceViewModel : DeviceViewModel
    {
        public Ups Ups { get; }

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

        public UpsDeviceViewModel(Ups ups)
        {
            this.Ups = ups;

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
                    () => FormatVoltage(this.Ups.InputVoltage)));

            this.PowerStatusLeftProperties.Add(
                new DevicePropertyViewModel(
                    "Input frequency:",
                    this.Ups,
                    nameof(this.Ups.InputFrequency),
                    () => FormatFrequency(this.Ups.InputFrequency)));

            this.PowerStatusLeftProperties.Add(
                new DevicePropertyViewModel(
                    "Input sensitivity:",
                    this.Ups,
                    nameof(this.Ups.InputSensitivity),
                    () => FormatEnum(this.Ups.InputSensitivity)));

            //
            // Power Status Right Column
            //
            this.PowerStatusRightProperties.Add(
                new DevicePropertyViewModel(
                    "Output voltage:",
                    this.Ups,
                    nameof(this.Ups.OutputVoltage),
                    () => FormatVoltage(this.Ups.OutputVoltage)));

            this.PowerStatusRightProperties.Add(
                new DevicePropertyViewModel(
                    "Output frequency:",
                    this.Ups,
                    nameof(this.Ups.OutputFrequency),
                    () => FormatFrequency(this.Ups.OutputFrequency)));

            //
            // Battery Status Left Column
            //
            this.BatteryStatusLeftProperties.Add(
                new DevicePropertyViewModel(
                    "Battery charge:",
                    this.Ups,
                    nameof(this.Ups.BatteryCharge),
                    () => FormatPercentage(this.Ups.BatteryCharge)));

            this.BatteryStatusLeftProperties.Add(
                new DevicePropertyViewModel(
                    "Battery current:",
                    this.Ups,
                    nameof(this.Ups.BatteryCurrent),
                    () => FormatCurrent(this.Ups.BatteryCurrent)));

            this.BatteryStatusLeftProperties.Add(
                new DevicePropertyViewModel(
                    "Battery replacement:",
                    this.Ups,
                    nameof(this.Ups.BatteryLastReplacement),
                    () => FormatDate(this.Ups.BatteryLastReplacement, "d")));

            //
            // Battery Status Left Column
            //
            this.BatteryStatusRightProperties.Add(
                new DevicePropertyViewModel(
                    "Battery runtime:",
                    this.Ups,
                    nameof(this.Ups.BatteryRuntime),
                    () => FormatMinutes(this.Ups.BatteryRuntime)));

            this.BatteryStatusRightProperties.Add(
                new DevicePropertyViewModel(
                    "Battery voltage:",
                    this.Ups,
                    nameof(this.Ups.BatteryVoltage),
                    () => FormatVoltage(this.Ups.BatteryVoltage)));

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
                    () => FormatTemperature(this.Ups.Temperature, true)));

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
                    () => FormatDate(this.Ups.ManufactureDate, "d")));

            this.DeviceInfoRightProperties.Add(
                new DevicePropertyViewModel(
                    "Driver version:",
                    this.Ups,
                    nameof(this.Ups.DriverName),
                    () => this.Ups.DriverName));
        }

        private string FormatVoltage(double? value)
        {
            if (value == null)
            {
                return null;
            }

            return $"{value.Value:###.0} V";
        }

        private string FormatCurrent(double? value)
        {
            if (value == null)
            {
                return null;
            }

            return $"{value.Value:##0.0} A";
        }

        private string FormatFrequency(double? value)
        {
            if (value == null)
            {
                return null;
            }

            return $"{value.Value:###.0} Hz";
        }

        private string FormatPercentage(double? value)
        {
            if (value == null)
            {
                return null;
            }

            return $"{value.Value:##0.0} %";
        }

        private string FormatDate(DateTime? value, string format)
        {
            return value?.ToString(format);
        }

        private string FormatMinutes(TimeSpan? value)
        {
            if (value == null)
            {
                return null;
            }

            return $"{value.Value.TotalMinutes} minutes";
        }

        private string FormatTemperature(double? value, bool convertCtoF)
        {
            if (value == null)
            {
                return null;
            }

            var temp = value.Value;

            if (convertCtoF)
            {
                temp = (temp * 9) / 5 + 32;
            }

            return $"{temp:###.0}\u00B0 F";
        }

        private string FormatEnum(Enum value)
        {
            return value?.ToString();
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
    }

    public class DevicePropertyViewModel : ViewModelBase
    {
        public string Header { get; }

        public DevicePropertyViewModel(
            string header, 
            Ups ups, 
            string propertyName,
            Func<string> converter)
        {
            this.Header = header;

            this.Value = converter();

            ups.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == propertyName)
                {
                    this.Value = converter();
                }
            };
        }

        private string value;

        public string Value
        {
            get => this.value;
            set => this.SetProperty(ref this.value, value);
        }
    }
}