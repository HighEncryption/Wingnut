namespace Wingnut.Data.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;

    [DataContract]
    public abstract class Device : INotifyPropertyChanged
    {
        private class DevicePropertyMetadata
        {
            public string PropertyName { get; set; }

            public PropertyInfo PropertyInfo { get; set; }

            public string ValueName { get; set; }

            public IDeviceValueConverter ValueConverter { get; set; }
        }

        [DataMember]
        private string deviceName;

        [DataMember]
        private Server server;

        public Server Server => this.server;

        private Dictionary<string, DevicePropertyMetadata> propertyMetadata;

        public void Initialize()
        {
            if (this.propertyMetadata != null)
            {
                return;
            }

            this.propertyMetadata = new Dictionary<string, DevicePropertyMetadata>();

            PropertyInfo[] allProperties =
                this.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (var property in allProperties)
            {
                DevicePropertyAttribute devPropAttribute =
                    property.GetCustomAttributes(true).OfType<DevicePropertyAttribute>().FirstOrDefault();

                if (devPropAttribute != null)
                {
                    DevicePropertyMetadata propertyData = new DevicePropertyMetadata()
                    {
                        PropertyName = property.Name,
                        PropertyInfo = property,
                        ValueName = devPropAttribute.PropertyName,
                    };

                    if (devPropAttribute.ConverterType != null)
                    {
                        propertyData.ValueConverter =
                            (IDeviceValueConverter) Activator.CreateInstance(devPropAttribute.ConverterType);
                    }

                    this.propertyMetadata.Add(devPropAttribute.PropertyName, propertyData);
                }
            }
        }

        protected Device(string name, Server server)
        {
            this.deviceName = name;
            this.server = server;

            this.Initialize();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate", Justification = "Protected method is specifically for raising the event.")]
        protected void RaisePropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        protected bool SetProperty<T>(ref T property, T newValue, [CallerMemberName] string propertyName = "")
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            // ReSharper disable RedundantNameQualifier
            if (object.Equals(property, default(T)) && object.Equals(newValue, default(T)))
            {
                return false;
            }

            if (!object.Equals(property, default(T)) && property.Equals(newValue))
            {
                return false;
            }

            // ReSharper restore RedundantNameQualifier
            property = newValue;

            RaisePropertyChanged(propertyName);

            return true;
        }

        internal void Update(Dictionary<string, string> dictionary)
        {
            foreach (KeyValuePair<string, string> pair in dictionary)
            {
                if (!this.propertyMetadata.TryGetValue(pair.Key, out DevicePropertyMetadata metadata))
                {
                    continue;
                }

                if (metadata.ValueConverter != null)
                {
                    metadata.PropertyInfo.SetValue(
                        this, 
                        metadata.ValueConverter.Convert(pair.Value));
                }
                else if (metadata.PropertyInfo.PropertyType == typeof(string))
                {
                    metadata.PropertyInfo.SetValue(this, pair.Value);
                }
                else if (metadata.PropertyInfo.PropertyType == typeof(double?))
                {
                    metadata.PropertyInfo.SetValue(this, double.Parse(pair.Value));
                }
                else if (metadata.PropertyInfo.PropertyType == typeof(DateTime?))
                {
                    metadata.PropertyInfo.SetValue(this, DateTime.Parse(pair.Value));
                }
                else if (metadata.PropertyInfo.PropertyType == typeof(TimeSpan?))
                {
                    double val = double.Parse(pair.Value);
                    metadata.PropertyInfo.SetValue(this, TimeSpan.FromSeconds(val));
                }
                else
                {
                    continue;
                }

                this.RaisePropertyChanged(metadata.PropertyName);
            }
        }

        protected void CloneFromMetadata(object source)
        {
            foreach (KeyValuePair<string, DevicePropertyMetadata> metadata in this.propertyMetadata)
            {
                var property = metadata.Value.PropertyInfo;
                property.SetValue(this, property.GetValue(source));
            }
        }

        public string QualifiedName =>
            string.Format(
                "{0}@{1}:{2}",
                this.Name,
                this.server.Address,
                this.server.Port);

        //internal List<string> UpdateVariables(Dictionary<string, string> vars)
        //{
        //    List<string> changedKeys = new List<string>();
        //    foreach (string key in vars.Keys)
        //    {
        //        if (this.VariableDictionary.TryGetValue(key, out string existingValue))
        //        {
        //            if (existingValue != vars[key])
        //            {
        //                changedKeys.Add(key);
        //                this.VariableDictionary[key] = vars[key];
        //            }
        //        }
        //        else
        //        {
        //            changedKeys.Add(key);
        //            this.VariableDictionary[key] = vars[key];
        //        }
        //    }

        //    return changedKeys;
        //}

        //public string GetString(string name)
        //{
        //    return this.VariableDictionary[name];
        //}

        //public double? GetDouble(string name)
        //{
        //    if (this.VariableDictionary.TryGetValue(name, out string value))
        //    {
        //        return double.Parse(value);
        //    }

        //    return null;
        //    //return double.Parse(this.VariableDictionary[name]);
        //}

        //public DateTime? GetDateTime(string name)
        //{
        //    if (this.VariableDictionary.TryGetValue(name, out string value) &&
        //        !string.IsNullOrWhiteSpace(value))
        //    {
        //        return DateTime.Parse(value);
        //    }

        //    return null;
        //}

        //public TimeSpan GetTimeSpan(string name, TimeSpanUnits units)
        //{
        //    double? value = this.GetDouble(name);

        //    if (value == null)
        //    {
        //        return TimeSpan.Zero;
        //    }

        //    switch (units)
        //    {
        //        case TimeSpanUnits.Seconds:
        //            return TimeSpan.FromSeconds(value.Value);
        //        case TimeSpanUnits.Minutes:
        //            return TimeSpan.FromMinutes(value.Value);
        //        default:
        //            throw new NotImplementedException("Unknown units " + units);
        //    }
        //}

        /// <summary>
        /// The name of the device as defined in the server's configuration
        /// </summary>
        public string Name => this.deviceName;

        [DataMember]
        [DeviceProperty("device.mfr")]
        public string Manufacturer { get; set; }

        [DataMember]
        [DeviceProperty("device.model")]
        public string Model { get; set; }

        [DataMember]
        [DeviceProperty("device.serial")]
        public string Serial { get; set; }

        [DataMember]
        [DeviceProperty("driver.name")]
        public string DriverName { get; set; }

        [DataMember]
        [DeviceProperty("driver.version")]
        public string DriverVersion { get; set; }

        private DateTime lastPollTime;

        [DataMember]
        public DateTime LastPollTime
        {
            get => this.lastPollTime;
            set => this.SetProperty(ref this.lastPollTime, value);
        }

        public abstract DeviceType DeviceType { get; }
    }
}