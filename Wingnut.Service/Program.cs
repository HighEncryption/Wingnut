namespace Wingnut.Service
{
    using System.ServiceProcess;

    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] servicesToRun = 
            {
                new ServiceCore()
            };

            ServiceBase.Run(servicesToRun);
        }
    }
}
