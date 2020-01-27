namespace Wingnut.Data.Models
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    public abstract class Device
    {
        [DataMember]
        protected Dictionary<string, string> VariableDictionary;

        protected Device(string name)
        {
            this.VariableDictionary = new Dictionary<string, string>();

            this.Name = name;
        }

        public string GetString(string name)
        {
            return this.VariableDictionary[name];
        }

        public double? GetDouble(string name)
        {
            if (this.VariableDictionary.TryGetValue(name, out string value))
            {
                return double.Parse(value);
            }

            return null;
            //return double.Parse(this.VariableDictionary[name]);
        }

        public DateTime GetDateTime(string name)
        {
            return DateTime.Parse(this.VariableDictionary[name]);
        }

        public TimeSpan GetTimeSpan(string name, TimeSpanUnits units)
        {
            double? value = this.GetDouble(name);

            if (value == null)
            {
                return TimeSpan.Zero;
            }

            switch (units)
            {
                case TimeSpanUnits.Seconds:
                    return TimeSpan.FromSeconds(value.Value);
                case TimeSpanUnits.Minutes:
                    return TimeSpan.FromMinutes(value.Value);
                default:
                    throw new NotImplementedException("Unknown units " + units);
            }
        }

        /// <summary>
        /// The name of the device as defined in the server's configuration
        /// </summary>
        public string Name { get;  }

        public string Manufacturer => this.GetString("device.mfr");
        public string Model => this.GetString("device.model");
        public string Serial => this.GetString("device.serial");
        public string DriverName => this.GetString("driver.name");

        [DataMember]
        public DateTime LastPollTime { get; set; }

        public abstract DeviceType DeviceType { get; }
    }
}