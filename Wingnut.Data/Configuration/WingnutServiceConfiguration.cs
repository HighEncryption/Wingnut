namespace Wingnut.Data.Configuration
{
    using System;
    using System.Net.Configuration;

    public class WingnutServiceConfiguration
    {
        // AKA DeadTime
        public int ServerNotRespondingTimeInSeconds { get; set; }

        // AKA HostSync
        public int NonresponsiveHostDelay { get; set; }

        // AKA MinSupplies
        public int MinimumPowerSupplies { get; set; }

        // AKA NoCommWarnTime
        public int NoCommNotifyDelayInSeconds { get; set; }

        // AKA POLLFREQ
        public int PollFrequencyInSeconds { get; set; }

        // AKA POLLFREQALERT
        public int PollFrequencyUrgentInSeconds { get; set; }

        // AKA RBWARNTIME
        public int ReplaceBatteryWarningTimeInSeconds { get; set; }

        public void ValidateProperties()
        {
            if (Math.Max(this.PollFrequencyInSeconds, this.PollFrequencyUrgentInSeconds) %
                this.ServerNotRespondingTimeInSeconds != 0)
            {
                throw new Exception(
                    string.Format(
                        "{0} must be a multiple {1} or {2}, which ever is larger",
                        nameof(ServerNotRespondingTimeInSeconds),
                        nameof(this.PollFrequencyInSeconds),
                        nameof(this.PollFrequencyUrgentInSeconds)));
            }

            if (this.MinimumPowerSupplies < 1)
            {
                throw new Exception(
                    string.Format(
                    "{0} must be greater than 0",
                    nameof(this.MinimumPowerSupplies)));
            }
        }

        public static WingnutServiceConfiguration CreateDefault()
        {
            return new WingnutServiceConfiguration
            {
                ServerNotRespondingTimeInSeconds = 15,
                MinimumPowerSupplies = 1,
                NoCommNotifyDelayInSeconds = 300,
                PollFrequencyInSeconds = 5,
                PollFrequencyUrgentInSeconds = 5,
                ReplaceBatteryWarningTimeInSeconds = 43200,
            };
        }
    }

    public class ShutdownConfiguration
    {
        public bool HibernateInsteadOfShutdown { get; set; }

        public bool AutoSignInNextBoot { get; set; }

        public int ShutdownDelayInSeconds { get; set; }

        public static ShutdownConfiguration CreateDefault()
        {
            return new ShutdownConfiguration
            {
                HibernateInsteadOfShutdown = false,
                AutoSignInNextBoot = false,
                ShutdownDelayInSeconds = 5,
            };
        }
    }

}