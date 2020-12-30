namespace Wingnut.Data.Configuration
{
    public class ShutdownConfiguration
    {
        public bool EnableShutdown { get; set; }

        public bool HibernateInsteadOfShutdown { get; set; }

        public bool AutoSignInNextBoot { get; set; }

        public int ShutdownDelayInSeconds { get; set; }

        public static ShutdownConfiguration CreateDefault()
        {
            return new ShutdownConfiguration
            {
                EnableShutdown = false,
                HibernateInsteadOfShutdown = false,
                AutoSignInNextBoot = false,
                ShutdownDelayInSeconds = 5,
            };
        }
    }
}