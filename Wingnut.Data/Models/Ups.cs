namespace Wingnut.Data.Models
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    public class Ups : Device
    {
        private double? batteryCharge;

        [DeviceProperty("battery.charge")]
        public double? BatteryCharge
        {
            get => this.batteryCharge;
            set => this.SetProperty(ref this.batteryCharge, value);
        }

        private DateTime? batteryLastReplacement;

        [DeviceProperty("battery.date")]
        public DateTime? BatteryLastReplacement
        {
            get => this.batteryLastReplacement;
            set => this.SetProperty(ref this.batteryLastReplacement, value);
        }

        private TimeSpan? batteryRuntime;

        [DeviceProperty("battery.runtime")]
        public TimeSpan? BatteryRuntime
        {
            get => this.batteryRuntime;
            set => this.SetProperty(ref this.batteryRuntime, value);
        }

        private TimeSpan? batteryRuntimeLow;

        [DeviceProperty("battery.runtime.low")]
        public TimeSpan? BatteryRuntimeLow
        {
            get => this.batteryRuntimeLow;
            set => this.SetProperty(ref this.batteryRuntimeLow, value);
        }

        //public double? InputFrequency => this.GetDouble("input.frequency");

        //public double? InputVoltage => this.GetDouble("input.voltage");

        //public double? OutputVoltage => this.GetDouble("output.voltage");

        //public double? OutputFrequency => this.GetDouble("output.frequency");

        //public double? OutputCurrent => this.GetDouble("output.current");

        //public double? LoadPercentage => this.GetDouble("ups.load");

        //public DeviceStatusType Status =>
        //    Constants.Device.ParseStatusString(this.GetString("ups.status"));

        //public DeviceStatusType Status { get; set; }

        private DeviceStatusType status;

        [DeviceProperty("ups.status", ConverterType = typeof(DeviceStatusConverter))]
        public DeviceStatusType Status
        {
            get => this.status;
            set => this.SetProperty(ref this.status, value);
        }

        public bool NotResponding { get; set; }

        public override DeviceType DeviceType => DeviceType.UPS;

        internal Ups Clone()
        {
            Ups clone = new Ups(this.Name, this.Server);

            clone.CloneFromMetadata(this);

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

        private Ups(string name, Server server)
            : base(name, server)
        {
        }
    }
}