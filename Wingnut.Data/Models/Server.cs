namespace Wingnut.Data.Models
{
    using System;
    using System.Management.Automation;
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
        public SSLUsage UseSSL { get; set; }

        [DataMember]
        public string ServerSSLName { get; set; }

        [DataMember]
        public AddressFamily? PreferredAddressFamily { get; set; }

        public string Name => $"{this.Address}:{this.Port}";

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
                UseSSL = serverConfiguration.UseSSL,
                ServerSSLName = serverConfiguration.ServerSSLName,
                PreferredAddressFamily = serverConfiguration.PreferredAddressFamily,
                ConnectionStatus = ServerConnectionStatus.NotConnected
            };
        }

        public override string ToString()
        {
            return this.Name;
        }
    }

    public enum DeviceType
    {
        Undefined,
        UPS
    }

    public enum TimeSpanUnits
    {
        Undefined,
        Seconds,
        Minutes
    }
}
