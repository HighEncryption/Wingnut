namespace Wingnut.Data.Configuration
{
    public class WingnutServiceConfiguration
    {
        // AKA DeadTime
        public int ServerNotRespondingTimeInSeconds { get; set; }

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
            // TODO
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
                ReplaceBatteryWarningTimeInSeconds = 43200
            };
        }
    }
}