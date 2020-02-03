namespace Wingnut.Console
{
    using System;

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

            ServiceRuntime.Instance.OnNotify += (sender, eventArgs) =>
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("NOTIFY {0} from device {1} on server {2}",
                    eventArgs.NotificationType,
                    eventArgs.UpsContext.Name,
                    eventArgs.UpsContext.UpsConfiguration.ServerConfiguration.DisplayName);
                Console.ResetColor();
            };

            // Example for attaching a PowerShell notification event
            //PowerShellNotifier psNotifier = new PowerShellNotifier("");
            //ServiceRuntime.Instance.OnNotify += psNotifier.HandleNotification;

            // Start the WCF service and monitoring task
            ServiceRuntime.Instance.Start();

            Console.WriteLine("Runtime is active. Press ENTER to terminate.");
            Console.ReadLine();

            ServiceRuntime.Instance.Stop();
        }
    }
}
