namespace Wingnut.Data.Models
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    public class Ups : Device
    {
        public double? BatteryCharge => this.GetDouble("battery.charge");

        public DateTime? BatteryLastReplacement => this.GetDateTime("battery.date");

        public TimeSpan? BatteryRuntime => this.GetTimeSpan("battery.runtime", TimeSpanUnits.Seconds);

        public TimeSpan? BatteryRuntimeLow => this.GetTimeSpan("battery.runtime.low", TimeSpanUnits.Seconds);

        public double? InputFrequency => this.GetDouble("input.frequency");

        public double? InputVoltage => this.GetDouble("input.voltage");

        public double? OutputVoltage => this.GetDouble("output.voltage");

        public double? OutputFrequency => this.GetDouble("output.frequency");

        public double? OutputCurrent => this.GetDouble("output.current");

        public double? LoadPercentage => this.GetDouble("ups.load");

        public DeviceStatusType Status =>
            Constants.Device.ParseStatusString(this.GetString("ups.status"));

        public bool NotResponding { get; set; }

        public override DeviceType DeviceType => DeviceType.UPS;

        internal Ups Clone()
        {
            return new Ups(
                this.Name,
                this.Server,
                new Dictionary<string, string>(this.VariableDictionary));
        }

        internal List<string> UpdateVariables(Dictionary<string, string> vars)
        {
            List<string> changedKeys = new List<string>();
            foreach (string key in vars.Keys)
            {
                if (this.VariableDictionary.TryGetValue(key, out string existingValue))
                {
                    if (existingValue != vars[key])
                    {
                        changedKeys.Add(key);
                        this.VariableDictionary[key] = vars[key];
                    }
                }
                else
                {
                    changedKeys.Add(key);
                    this.VariableDictionary[key] = vars[key];
                }
            }

            return changedKeys;
        }

        public Ups(string name, Server server, Dictionary<string, string> variableDictionary)
            : base(name, server)
        {
            foreach (KeyValuePair<string, string> pair in variableDictionary)
            {
                this.VariableDictionary.Add(pair.Key, pair.Value);
            }
        }
    }
}