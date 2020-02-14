namespace Wingnut.Data.Models
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    public enum SensitivityType
    {
        Undefined,
        Low,
        Medium,
        High
    }

    [DataContract]
    public class Ups : Device
    {
        #region Device Properties

        private double? batteryCharge;

        [DataMember]
        [DeviceProperty("battery.charge")]
        public double? BatteryCharge
        {
            get => this.batteryCharge;
            set => this.SetProperty(ref this.batteryCharge, value);
        }

        private double? batteryCurrent;

        [DataMember]
        [DeviceProperty("battery.current")]
        public double? BatteryCurrent
        {
            get => this.batteryCurrent;
            set => this.SetProperty(ref this.batteryCurrent, value);
        }

        private DateTime? batteryLastReplacement;

        [DataMember]
        [DeviceProperty("battery.date")]
        public DateTime? BatteryLastReplacement
        {
            get => this.batteryLastReplacement;
            set => this.SetProperty(ref this.batteryLastReplacement, value);
        }

        private TimeSpan? batteryRuntime;

        [DataMember]
        [DeviceProperty("battery.runtime")]
        public TimeSpan? BatteryRuntime
        {
            get => this.batteryRuntime;
            set => this.SetProperty(ref this.batteryRuntime, value);
        }

        private double? batteryVoltage;

        [DataMember]
        [DeviceProperty("battery.voltage")]
        public double? BatteryVoltage
        {
            get => this.batteryVoltage;
            set => this.SetProperty(ref this.batteryVoltage, value);
        }

        private TimeSpan? batteryRuntimeLow;

        [DataMember]
        [DeviceProperty("battery.runtime.low")]
        public TimeSpan? BatteryRuntimeLow
        {
            get => this.batteryRuntimeLow;
            set => this.SetProperty(ref this.batteryRuntimeLow, value);
        }

        private double? inputFrequency;

        [DataMember]
        [DeviceProperty("input.frequency")]
        public double? InputFrequency
        {
            get => this.inputFrequency;
            set => this.SetProperty(ref this.inputFrequency, value);
        }

        private double? inputVoltage;

        [DataMember]
        [DeviceProperty("input.voltage")]
        public double? InputVoltage
        {
            get => this.inputVoltage;
            set => this.SetProperty(ref this.inputVoltage, value);
        }

        private SensitivityType? inputSensitivity;

        public SensitivityType? InputSensitivity
        {
            get => this.inputSensitivity;
            set => this.SetProperty(ref this.inputSensitivity, value);
        }

        private double? outputFrequency;

        [DataMember]
        [DeviceProperty("output.frequency")]
        public double? OutputFrequency
        {
            get => this.outputFrequency;
            set => this.SetProperty(ref this.outputFrequency, value);
        }

        private double? outputVoltage;

        [DataMember]
        [DeviceProperty("output.voltage")]
        public double? OutputVoltage
        {
            get => this.outputVoltage;
            set => this.SetProperty(ref this.outputVoltage, value);
        }

        private double? outputCurrent;

        [DataMember]
        [DeviceProperty("output.current")]
        public double? OutputCurrent
        {
            get => this.outputCurrent;
            set => this.SetProperty(ref this.outputCurrent, value);
        }

        private double? loadPercentage;

        [DataMember]
        [DeviceProperty("ups.load")]
        public double? LoadPercentage
        {
            get => this.loadPercentage;
            set => this.SetProperty(ref this.loadPercentage, value);
        }

        private DeviceStatusType status;

        [DataMember]
        [DeviceProperty("ups.status", ConverterType = typeof(DeviceStatusConverter))]
        public DeviceStatusType Status
        {
            get => this.status;
            set => this.SetProperty(ref this.status, value);
        }

        private double? temperature;

        [DataMember]
        [DeviceProperty("ups.temperature")]
        public double? Temperature
        {
            get => this.temperature;
            set => this.SetProperty(ref this.temperature, value);
        }

        private string firmware;

        [DataMember]
        [DeviceProperty("ups.firmware")]
        public string Firmware
        {
            get => this.firmware;
            set => this.SetProperty(ref this.firmware, value);
        }

        private DateTime? manufactureDate;

        [DataMember]
        [DeviceProperty("ups.mfr.date")]
        public DateTime? ManufactureDate
        {
            get => this.manufactureDate;
            set => this.SetProperty(ref this.manufactureDate, value);
        }

        #endregion

        public bool NotResponding { get; set; }

        public override DeviceType DeviceType => DeviceType.UPS;

        internal Ups Clone()
        {
            Ups clone = new Ups(this.Name, this.Server);
            clone.UpdateVariables(this);
            return clone;
        }

        public static Ups Create(
            string name,
            Server server,
            Dictionary<string, string> variableDictionary)
        {
            Ups ups = new Ups(name, server);
            ups.Update(variableDictionary);
            return ups;
        }

        public void UpdateVariables(Ups ups)
        {
            base.CloneFromMetadata(ups);
        }

        private Ups(string name, Server server)
            : base(name, server)
        {
        }
    }
}