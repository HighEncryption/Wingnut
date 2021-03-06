﻿namespace Wingnut.Data.Configuration
{
    using Wingnut.Data.Models;

    public class UpsConfiguration
    {
        public ServerConfiguration ServerConfiguration { get; set; }

        public string DeviceName { get; set; }

        public bool MonitorOnly { get; set; }

        public bool EnableEmailNotification { get; set; }

        public bool EnablePowerShellNotification { get; set; }

        /// <summary>
        /// The number of power supplies on this device being powered by this UPS. This is
        /// equivalent to the 'power value' settings in ups.conf.
        /// </summary>
        public int NumPowerSupplies { get; set; }

        public int BatteryRuntimeLowOverride { get; set; }

        public static UpsConfiguration Create(Ups ups)
        {
            return new UpsConfiguration
            {
                ServerConfiguration = new ServerConfiguration
                {
                    Address = ups.Server.Address,
                    Port = ups.Server.Port,
                    Username = ups.Server.Username,
                    Password = ups.Server.Password,
                    UseSSL = ups.Server.UseSSL,
                    ServerSSLName = ups.Server.ServerSSLName,
                    PreferredAddressFamily = ups.Server.PreferredAddressFamily,
                },
                DeviceName = ups.Name,
                EnableEmailNotification = true,
                EnablePowerShellNotification = true
            };
        }

        public void ValidateProperties()
        {
            this.ServerConfiguration.ValidateProperties();

            if (this.NumPowerSupplies < 1 || this.NumPowerSupplies > 16)
            {
                throw new WingnutException(
                    $"NumPowerSupplies is out of range ({this.NumPowerSupplies})");
            }
        }

        public string GetQualifiedName()
        {
            return string.Format(
                "{0}@{1}:{2}",
                this.DeviceName,
                this.ServerConfiguration.Address,
                this.ServerConfiguration.Port);

        }
}
}