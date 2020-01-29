namespace Wingnut.Data.Configuration
{
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

        public SSLUsage UseSSL { get; set; }

        public string SSLTargetName { get; set; }

        public AddressFamily? PreferredAddressFamily { get; set; }

        public string DisplayName => $"{this.Username}@{this.Address}:{this.Port}";

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
        }

        public static ServerConfiguration CreateFromServer(Server server)
        {
            return new ServerConfiguration
            {
                Address = server.Address,
                Port = server.Port,
                Username = server.Username,
                Password = server.Password,
                UseSSL = server.UseSSL,
                SSLTargetName = server.SSLTargetName,
                PreferredAddressFamily = server.PreferredAddressFamily,
            };
        }
    }
}
