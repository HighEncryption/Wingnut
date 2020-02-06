namespace Wingnut.Core
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Mail;
    using System.Text;

    using Wingnut.Tracing;

    public class SmtpNotifier : INotificationHandler
    {
        public void HandleNotification(object sender, NotifyEventArgs eventArgs)
        {
            var config = 
                ServiceRuntime.Instance.Configuration.ServiceConfiguration.SmtpConfiguration;

            if (config == null)
            {
                Logger.Error("SmtpNotifier: No configuration provided");
                return;
            }

            string[] notificationTypes = config.NotificationTypes.Split(',');

            if (!notificationTypes.Any(
                n => string.Equals(
                    n,
                    eventArgs.NotificationType.ToString(),
                    StringComparison.OrdinalIgnoreCase)))
            {
                Logger.Info(
                    "Notification type {0} not enabled for SMTP notification", 
                    eventArgs.NotificationType);

                return;
            }

            string messageSubject = string.Format(
                "UPS {0} has status {1}",
                eventArgs.UpsContext.Name,
                eventArgs.NotificationType);

            // TODO
            string messageBody = "TODO";

            using (SmtpClient client = new SmtpClient())
            {
                client.Host = config.Host;
                client.Port = config.Port;
                client.EnableSsl = config.EnableSSL;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.Credentials = new NetworkCredential()
                {
                    SecurePassword = config.Password,
                    UserName = config.Username
                };

                MailMessage message = new MailMessage(
                    config.FromAddress,
                    config.ToAddresses,
                    messageSubject,
                    messageBody)
                {
                    BodyEncoding = Encoding.UTF8
                };

                client.Send(message);
            }
        }
    }
}