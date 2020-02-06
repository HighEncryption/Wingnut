namespace Wingnut.Data.Configuration
{
    using System.Collections.Generic;
    using System.Security;

    using Newtonsoft.Json;

    public class SmtpConfiguration
    {
        public string Host { get; set; }

        public int Port { get; set; }

        public bool EnableSSL { get; set; }

        public string Username { get; set; }

        [JsonConverter(typeof(SecureStringToProtectedDataConverter))]
        public SecureString Password { get; set; }

        public string FromAddress { get; set; }

        public string ToAddresses { get; set; }

        public string NotificationTypes { get; set; }

        public SmtpConfiguration()
        {
            var defaultTypes = new List<string>()
            {
                nameof(NotificationType.OnBattery),
                nameof(NotificationType.LowBattery),
                nameof(NotificationType.ReplaceBattery),
                nameof(NotificationType.Shutdown),
            };

            this.NotificationTypes = string.Join(",", defaultTypes);
        }
    }
}