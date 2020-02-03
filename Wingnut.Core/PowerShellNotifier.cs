﻿namespace Wingnut.Core
{
    using System.IO;
    using System.Management.Automation;

    using Wingnut.Tracing;

    public class PowerShellNotifier
    {
        private readonly string scriptPath;

        public PowerShellNotifier(string scriptPath)
        {
            this.scriptPath = scriptPath;
        }

        public void HandleNotification(object sender, NotifyEventArgs eventArgs)
        {
            if (!File.Exists(this.scriptPath))
            {
                Logger.NotificationScriptNotFound(this.scriptPath);
                Logger.Error(
                    "The PowerShell notification script was not found at the path '{0}'", 
                    this.scriptPath);

                return;
            }

            string scriptContent = File.ReadAllText(this.scriptPath);

            Logger.Debug(
                "PowerShellNotifier: Calling Receive-WingnutNotification with Type={0}, Ups={1}, ScriptPath={2}",
                eventArgs.NotificationType,
                eventArgs.UpsContext.QualifiedName,
                this.scriptPath);

            // Reference: https://docs.microsoft.com/en-us/archive/blogs/kebab/executing-powershell-scripts-from-c
            using (PowerShell pipeline = PowerShell.Create())
            {
                pipeline.Streams.Error.DataAdded += (o, args) =>
                    Logger.Error(
                        "PowerShellNotifier: {0}", 
                        ((PSDataCollection<ErrorRecord>) sender)[args.Index].ToString());

                pipeline.Streams.Warning.DataAdded += (o, args) =>
                    Logger.Warning(
                        "PowerShellNotifier: {0}",
                        ((PSDataCollection<WarningRecord>)sender)[args.Index].ToString());

                pipeline.Streams.Information.DataAdded += (o, args) =>
                    Logger.Info(
                        "PowerShellNotifier: {0}",
                        ((PSDataCollection<InformationRecord>)sender)[args.Index].ToString());

                pipeline.Streams.Verbose.DataAdded += (o, args) =>
                    Logger.Debug(
                        "PowerShellNotifier: {0}",
                        ((PSDataCollection<VerboseRecord>)sender)[args.Index].ToString());

                pipeline.Streams.Debug.DataAdded += (o, args) =>
                    Logger.Debug(
                        "PowerShellNotifier: {0}",
                        ((PSDataCollection<DebugRecord>)sender)[args.Index].ToString());

                // Add the contents of the script to the pipeline to be invoked
                pipeline.AddScript(scriptContent);

                pipeline.Invoke();

                pipeline.Commands.Clear();

                var command = pipeline.AddCommand("Receive-WingnutNotification");
                command.AddParameter("notificationType", eventArgs.NotificationType);
                command.AddParameter("ups", eventArgs.UpsContext.State);

                pipeline.Invoke();
            }

            Logger.Debug("PowerShellNotifier: Finished Receive-WingnutNotification");
        }
    }
}