﻿namespace Wingnut.Console
{
    using System;
    using System.Data.SQLite;

    using Wingnut.Core;
    using Wingnut.Tracing;

    using Console = System.Console;

    class Program
    {
        static void Main(string[] args)
        {
            // This reference is needed to ensure that the sqlite binaries get copied to
            // the output directory correctly.
#pragma warning disable 168
            SQLiteConnection connection;
#pragma warning restore 168

            Logger.SetOutputType(Logger.OutputType.Console);
            Logger.OutputLogLevel = Logger.LogLevel.Debug;

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

            // Start the WCF service and monitoring task
            ServiceRuntime.Instance.Start();

            Console.WriteLine("Runtime is active. Press ENTER to terminate.");
            Console.ReadLine();

            ServiceRuntime.Instance.Stop();
        }
    }
}
