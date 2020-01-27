namespace Wingnut.Console
{
    using Wingnut.Core;
    using Wingnut.Tracing;

    using Console = System.Console;

    class Program
    {
        static void Main(string[] args)
        {
            Logger.SetOutputType(Logger.OutputType.Console);

            // Initialize the runtime (load configuration, etc.)
            ServiceRuntime.Instance.Initialize();

            // Start the WCF service and monitoring task
            ServiceRuntime.Instance.Start();

            Console.WriteLine("Runtime is active. Press ENTER to terminate.");
            Console.ReadLine();

            ServiceRuntime.Instance.Stop();
        }
    }
}
