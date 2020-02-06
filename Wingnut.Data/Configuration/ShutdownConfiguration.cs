namespace Wingnut.Data.Configuration
{
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