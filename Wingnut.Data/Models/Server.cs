namespace Wingnut.Data.Models
{
    using System;
    using System.Collections.Generic;
    using System.Net.Sockets;
    using System.Runtime.Serialization;
    using System.Security;

    using Wingnut.Data.Configuration;

    public enum ServerConnectionStatus
    {
        Undefined,

        /// <summary>
        /// A connection has not yet been made to the server
        /// </summary>
        NotConnected,

        /// <summary>
        /// The server is currently connected
        /// </summary>
        Connected,

        /// <summary>
        /// The server was previously connection but has lost connection
        /// </summary>
        LostConnection
    }

    [DataContract]
    public class Server
    {
        [DataMember]
        public string Address { get; set; }

        [DataMember]
        public int Port { get; set; }

        [DataMember]
        public string Username { get; set; }

        [DataMember]
        public SecureString Password { get; set; }

        [DataMember]
        public int PollFrequencyInSeconds { get; set; }

        [DataMember]
        public SSLUsage UseSSL { get; set; }

        public string SSLTargetName { get; set; }

        [DataMember]
        public AddressFamily? PreferredAddressFamily { get; set; }

        public string Name => $"{this.Username}@{this.Address}:{this.Port}";

        [DataMember]
        public ServerConnectionStatus ConnectionStatus { get; set; }

        [DataMember]
        public DateTime LastConnectionTime { get; set; }

        [DataMember]
        public TimeSpan ConnectionTimeout { get; set; }

        public static Server CreateFromConfiguration(
            ServerConfiguration serverConfiguration)
        {
            return new Server
            {
                Address = serverConfiguration.Address,
                Port = serverConfiguration.Port,
                Username = serverConfiguration.Username,
                Password = serverConfiguration.Password,
                PollFrequencyInSeconds = serverConfiguration.PollFrequencyInSeconds,
                UseSSL = serverConfiguration.UseSSL,
                ConnectionStatus = ServerConnectionStatus.NotConnected
            };
        }
    }

    public enum DeviceType
    {
        Undefined,
        UPS
    }

    [DataContract]
    public class Ups : Device
    {
        public double? BatteryCharge => this.GetDouble("battery.charge");

        public DateTime BatteryLastReplacement => this.GetDateTime("battery.date");

        public TimeSpan BatteryRuntime => this.GetTimeSpan("battery.runtime", TimeSpanUnits.Seconds);

        public TimeSpan BatteryRuntimeLow => this.GetTimeSpan("battery.runtime.low", TimeSpanUnits.Seconds);

        public double? InputFrequency => this.GetDouble("input.frequency");

        public double? InputVoltage => this.GetDouble("input.voltage");

        public double? OutputVoltage => this.GetDouble("output.voltage");

        public double? OutputFrequency => this.GetDouble("output.frequency");

        public double? OutputCurrent => this.GetDouble("output.current");

        public double? LoadPercentage => this.GetDouble("ups.load");

        public DeviceStatusType Status =>
            DeviceConstants.GetStatusType(this.GetString("ups.status"));

        public override DeviceType DeviceType => DeviceType.UPS;

        // TODO: Make internal
        public List<string> UpdateVariables(Dictionary<string, string> vars)
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

        public Ups(string name, Dictionary<string, string> variableDictionary)
            : base(name)
        {
            foreach (KeyValuePair<string, string> pair in variableDictionary)
            {
                this.VariableDictionary.Add(pair.Key, pair.Value);
            }
        }
    }

    public enum TimeSpanUnits
    {
        Undefined,
        Seconds,
        Minutes
    }
}
