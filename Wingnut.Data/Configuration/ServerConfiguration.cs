namespace Wingnut.Data.Configuration
{
    using System.Collections.Generic;
    using System.Net.Sockets;
    using System.Security;

    using Newtonsoft.Json;

    using Wingnut.Data.Models;

    public class ServerConfiguration
    {
        public string Address { get; set; }

        public int Port { get; set; }

        public string Username { get; set; }

        [JsonConverter(typeof(SecureStringToProtectedDataConverter))]
        public SecureString Password { get; set; }

        public int PollFrequencyInSeconds { get; set; }

        public SSLUsage UseSSL { get; set; }

        public string SSLTargetName { get; set; }

        public AddressFamily? PreferredAddressFamily { get; set; }

        public List<UpsConfiguration> Upses { get; set; }

        public int NoCommNotifyDelayInSeconds { get; set; }

        public string Name => $"{this.Username}@{this.Address}:{this.Port}";

        public ServerConfiguration()
        {
            this.Upses = new List<UpsConfiguration>();
            this.NoCommNotifyDelayInSeconds = 300;
        }

        public static ServerConfiguration Create(Server server)
        {
            return new ServerConfiguration
            {
                Address =  server.Address,
                Port = server.Port,
                Username = server.Username,
                Password = server.Password,
                PollFrequencyInSeconds = server.PollFrequencyInSeconds,
                UseSSL = server.UseSSL,
                SSLTargetName = server.SSLTargetName,
                PreferredAddressFamily = server.PreferredAddressFamily,
            };
        }

        public void ValidateProperties()
        {
            if (string.IsNullOrEmpty(this.Address))
            {
                throw new WingnutException("The Address cannot be empty");
            }

            if (this.Port == 0)
            {
                throw new WingnutException("The port number cannot be 0");
            }

            if (string.IsNullOrWhiteSpace(this.Username))
            {
                throw new WingnutException("The Username cannot be empty");
            }

            if (Password == null || Password.Length == 0)
            {
                throw new WingnutException("The Password cannot be empty");
            }

            if (this.PollFrequencyInSeconds <= 0 || this.PollFrequencyInSeconds > 60)
            {
                throw new WingnutException("The PollFrequencyInSeconds is out of range");
            }
        }
    }

    public class UpsConfiguration
    {
        public string Name { get; set; }

        public bool MonitorOnly { get; set; }

        /// <summary>
        /// The number of power supplies on this device being powered by this UPS. This is
        /// equivalent to the 'power value' settings in ups.conf.
        /// </summary>
        public int NumPowerSupplies { get; set; }

        public int BatterRuntimeLowOverride { get; set; }
    }
}
