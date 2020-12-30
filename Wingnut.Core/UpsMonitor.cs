namespace Wingnut.Core
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Wingnut.Data;
    using Wingnut.Data.Configuration;
    using Wingnut.Data.Models;
    using Wingnut.Tracing;

    public enum MonitorStatus
    {
        Undefined,
        Running,
        Stopped,
        Faulted
    }

    public class UpsMonitor
    {
        private CancellationTokenSource cancellationTokenSource;
        private Task monitorTask;

        public MonitorStatus Status { get; private set; }

        public BlockingCollection<UpsStatusChangeData> Changes { get; private set; }

        public int ActivePowerValue { get; internal set; }

        public void Start()
        {
            this.cancellationTokenSource = new CancellationTokenSource();
            this.Changes = new BlockingCollection<UpsStatusChangeData>();

            monitorTask = Task.Run(async () => await this.UpsMonitorMain().ConfigureAwait(false));
        }

        public void Stop()
        {
            this.cancellationTokenSource.Cancel();
            this.monitorTask.Wait();
        }

        /// <summary>
        /// The synchronization object used gate access to process status changes for UPS devices.
        /// </summary>
        /// <remarks>
        /// The reader/writer locking pattern is used here so that updates can be made to UPSes
        /// concurrently (by acquiring a reader lock), however processing those updates should be
        /// done while other updates cannot be made (by acquiring the writer lock).
        /// </remarks>
        internal readonly AsyncReaderWriterLock ReaderWriterLock = new AsyncReaderWriterLock();

        private async Task UpsMonitorMain()
        {
            // TODO: Call LOGIN to the server so that it knows we are listening
            try
            {
                this.InitializeFromConfiguration();

                this.Status = MonitorStatus.Running;

                while (!this.cancellationTokenSource.IsCancellationRequested)
                {
                    foreach (UpsContext upsContext in ServiceRuntime.Instance.UpsContexts)
                    {
                        // Start (or restart) any monitoring tasks
                        if (upsContext.MonitoringTask == null ||
                            upsContext.MonitoringTask.IsFaulted)
                        {
                            upsContext.StartMonitoring(
                                this,
                                this.cancellationTokenSource.Token);
                        }
                    }

                    if (this.Changes.TryTake(out UpsStatusChangeData changeData, 5000))
                    {
                        Logger.Debug("UpsMonitorMain: Received signal to process device status change");

                        // The status of a UPS has changed, so wait until we have exclusive access
                        // to all UPSes then process the change. 
                        using (await this.ReaderWriterLock.WriterLockAsync())
                        {
                            // We have exclusive access to process status changes, so we need to be
                            // as efficient as possible in this area.
                            ProcessUpsStatusChanges(changeData);
                        }
                    }
                }

                this.Status = MonitorStatus.Stopped;
            }
            catch (Exception e)
            {
                this.Status = MonitorStatus.Faulted;

                Console.WriteLine(e);
                throw;
            }
        }

        private void ProcessUpsStatusChanges(UpsStatusChangeData changeData)
        {
            Logger.Debug("ProcessUpsStatusChanges: Starting.");

            // Process the changes and send notification for the changes for this UPS
            this.ProcessSingleUpsStatusChange(changeData);

            // Calculate the current power value to determine if any actions need to be taken
            int newActivePowerValue = 0;
            foreach (UpsContext upsContext in ServiceRuntime.Instance.UpsContexts)
            {
                Logger.Debug($"Calculating power value for UPS {upsContext.QualifiedName}");

                if (upsContext.State == null)
                {
                    Logger.Debug($"Skipping UPS {upsContext.QualifiedName} since context has not yet been initialized");
                    continue;
                }

                if (upsContext.UpsConfiguration.MonitorOnly)
                {
                    Logger.Debug("Skipping UPS. Ups is configured as MonitorOnly");
                    continue;
                }

                if (upsContext.State.Status.HasFlag(DeviceStatusType.OnBattery) &&
                    upsContext.State.Status.HasFlag(DeviceStatusType.LowBattery))
                {
                    Logger.Debug("Skipping UPS. State=OnBattery&LowBattery");
                    continue;
                }

                // If the UPS is considered dead (due to inability to contact it) and the last 
                // known status of the UPS was OnBattery, assume that the UPS is in a LowBattery
                // state and do not count it towards the power value.
                if (IsUpsDead(upsContext) &&
                    upsContext.State.Status.HasFlag(DeviceStatusType.OnBattery))
                {
                    Logger.Debug("Skipping UPS. UPS is dead and State=OnBattery.");
                    continue;
                }

                Logger.Debug(
                    $"Finished calculating power value. Adding power value of {upsContext.UpsConfiguration.NumPowerSupplies}");

                newActivePowerValue += upsContext.UpsConfiguration.NumPowerSupplies;
            }

            if (newActivePowerValue == this.ActivePowerValue)
            {
                Logger.Debug("Power value is unchanged ({0})", this.ActivePowerValue);
            }
            else
            {
                Logger.Info(
                    "ActivePowerValue changing from {0} to {1}",
                    this.ActivePowerValue,
                    newActivePowerValue);

                this.ActivePowerValue = newActivePowerValue;
            }

            if (this.ActivePowerValue <
                ServiceRuntime.Instance.Configuration.ServiceConfiguration.MinimumPowerSupplies)
            {
                Logger.PowerValueBelowThreshold(
                    this.ActivePowerValue,
                    ServiceRuntime.Instance.Configuration.ServiceConfiguration.MinimumPowerSupplies);

                this.InitiateShutdown();
            }

            Logger.Debug("ProcessUpsStatusChanges: Finished.");
        }

        /// <summary>
        /// Process the status change for a single UPS. This method only logs events and performs
        /// notification. No actions are taken by this method.
        /// </summary>
        /// <param name="changeData">The change data</param>
        private void ProcessSingleUpsStatusChange(UpsStatusChangeData changeData)
        {
            // Alias the UpsContext to make the code here a bit cleaner
            UpsContext ctx = changeData.UpsContext;

            // First check if we do not have connectivity to the UPS, since we will want to
            // skip processing other state checks if this is the case
            if (ctx.ServerState.ConnectionStatus == ServerConnectionStatus.LostConnection)
            {
                if (ctx.LastNoCommNotifyTime == null)
                {
                    string error = changeData.Exception == null
                        ? "(none)"
                        : changeData.Exception.Message;

                    Logger.CommunicationLost(ctx.Name, ctx.ServerState.Name, error);

                    ServiceRuntime.Instance.Notify(ctx, NotificationType.CommunicationLost);

                    ctx.LastNoCommNotifyTime = DateTime.Now;
                }
                else if (
                    ctx.LastNoCommNotifyTime.Value.OlderThan(
                        TimeSpan.FromSeconds(
                            ServiceRuntime.Instance.Configuration.ServiceConfiguration.NoCommNotifyDelayInSeconds)))
                {
                    string error = changeData.Exception == null
                        ? "(none)"
                        : changeData.Exception.Message;

                    Logger.NoCommunication(ctx.Name, ctx.ServerState.Name, error);

                    ServiceRuntime.Instance.Notify(
                        ctx,
                        NotificationType.NoCommunication);

                    ctx.LastNoCommNotifyTime = DateTime.Now;
                }
            }
            else if (ctx.ServerState.ConnectionStatus == ServerConnectionStatus.Connected &&
                     ctx.LastNoCommNotifyTime != null)
            {
                ServiceRuntime.Instance.Notify(
                    ctx,
                    NotificationType.CommunicationRestored);

                Logger.CommunicationRestored(ctx.Name, ctx.ServerState.Name);

                Logger.Info("Communication with UPS {0} was restored", ctx.QualifiedName);

                ctx.LastNoCommNotifyTime = null;
            }

            if (IsStatusTransition(changeData, DeviceStatusType.Online, true))
            {
                // The device is going from !Online to Online
                Logger.UpsOnline(ctx.Name, ctx.ServerState.Name);
                ServiceRuntime.Instance.Notify(ctx, NotificationType.Online);
            }

            if (IsStatusTransition(changeData, DeviceStatusType.OnBattery, false))
            {
                // The device is going from !OnBattery to OnBattery
                Logger.UpsOnBattery(ctx.Name, ctx.ServerState.Name);
                ServiceRuntime.Instance.Notify(ctx, NotificationType.OnBattery);
            }

            if (IsStatusTransition(changeData, DeviceStatusType.LowBattery, true))
            {
                // The device is going from !LowBattery to LowBattery
                Logger.UpsLowBattery(ctx.Name, ctx.ServerState.Name);
                ServiceRuntime.Instance.Notify(ctx, NotificationType.LowBattery);
            }

            if (ctx.State.Status.HasFlag(DeviceStatusType.ReplaceBattery))
            {
                // The ReplaceBattery status is set. Check if we haven't yet logged an event
                // and notified for this, or if we have waited enough time to do it again.
                if (ctx.LastReplaceBatteryWarnTime == null ||
                    ctx.LastReplaceBatteryWarnTime.Value.OlderThan(
                        TimeSpan.FromSeconds(
                            ServiceRuntime.Instance.Configuration.ServiceConfiguration
                                .ReplaceBatteryWarningTimeInSeconds)))
                {
                    Logger.UpsReplaceBattery(
                        ctx.Name,
                        ctx.ServerState.Name,
                        ctx.State.BatteryLastReplacement);

                    Logger.Info(
                        "The UPS {0} is reporting that the battery should be replaced. The last replacement date is {1}",
                        ctx.QualifiedName,
                        ctx.State.BatteryLastReplacement);

                    ServiceRuntime.Instance.Notify(
                        ctx,
                        NotificationType.ReplaceBattery);

                    ctx.LastReplaceBatteryWarnTime = DateTime.Now;
                }
            }

            DeviceSeverityType severity = Constants.Device.GetStatusSeverity(
                ctx.State.Status);

            if (changeData.PreviousState == null)
            {
                // This is the very first time polling this device, so log the initial status
                var newSetFlags = EnumExtensions.GetSetFlagNames<DeviceStatusType>(
                    ctx.State.Status);

                Logger.Log(
                    Constants.Device.DeviceSeverityToLogLevel(severity),
                    "Initial status of UPS {0} is {1}",
                    ctx.QualifiedName,
                    string.Join("|", newSetFlags));
            }
            else if (ctx.State.Status != changeData.PreviousState.Status)
            {
                // The status of the UPS has changed, so log the previous and new status
                var initialSetFlags = EnumExtensions.GetSetFlagNames<DeviceStatusType>(
                    changeData.PreviousState.Status);

                var newSetFlags = EnumExtensions.GetSetFlagNames<DeviceStatusType>(
                    ctx.State.Status);

                Logger.Log(
                    Constants.Device.DeviceSeverityToLogLevel(severity),
                    "Status of UPS {0} has changed from {1} to {2}",
                    ctx.QualifiedName,
                    string.Join("|", initialSetFlags),
                    string.Join("|", newSetFlags));
            }
        }

        private static bool IsStatusTransition(
            UpsStatusChangeData changeData, 
            DeviceStatusType status,
            bool requirePreviousState)
        {
            if (!requirePreviousState)
            {
                return changeData.UpsContext.State.Status.HasFlag(status) &&
                       (changeData.PreviousState == null ||
                        !changeData.PreviousState.Status.HasFlag(status));
            }

            if (changeData.PreviousState == null)
            {
                return false;
            }

            return changeData.UpsContext.State.Status.HasFlag(status) &&
                   !changeData.PreviousState.Status.HasFlag(status);
        }

        private static bool IsUpsDead(UpsContext upsContext)
        {
            // If we lost communication with the UPS and it has been longer than DEADTIME seconds
            // and the last known status is OnBattery, assume that the UPS is dead.

            return upsContext.ServerState.ConnectionStatus == ServerConnectionStatus.LostConnection &&
                   upsContext.LastNoCommNotifyTime.HasValue &&
                   upsContext.LastNoCommNotifyTime.Value.OlderThan(
                       TimeSpan.FromSeconds(
                           ServiceRuntime.Instance.Configuration.ServiceConfiguration
                               .ServerNotRespondingTimeInSeconds));
        }

        private void InitializeFromConfiguration()
        {
            foreach (UpsConfiguration upsConfiguration in 
                ServiceRuntime.Instance.Configuration.UpsConfigurations)
            {
                Logger.Info($"Loading server {upsConfiguration.DeviceName} from configuration");

                try
                {
                    upsConfiguration.ValidateProperties();
                }
                catch (Exception exception)
                {
                    Logger.Error(
                        "An error occurred while loading the configuration for the UPS '{0}' on server '{1}. The error was: {2}",
                        upsConfiguration.DeviceName,
                        upsConfiguration.ServerConfiguration.DisplayName,
                        exception.Message);

                    Logger.Info("Exception during upsContext load. {0}", exception);
                }

                if (ServiceRuntime.Instance.UpsContexts.Any(
                    s => string.Equals(
                        s.QualifiedName, 
                        upsConfiguration.GetQualifiedName())))
                {
                    throw new WingnutException("The device already exists");
                }

                Server server = Server.CreateFromConfiguration(
                    upsConfiguration.ServerConfiguration);

                UpsContext upsContext = new UpsContext(
                    upsConfiguration,
                    server);

                ServiceRuntime.Instance.UpsContexts.Add(upsContext);

                Logger.Info($"Ups '{upsContext.Name}' successfully loaded from configuration");
            }

            Logger.Info(
                "Finished loading {0} devices from configuration",
                ServiceRuntime.Instance.Configuration.UpsConfigurations.Count);
        }

        private void InitiateShutdown()
        {
            ShutdownConfiguration shutdownConfig =
                ServiceRuntime.Instance.Configuration.ShutdownConfiguration;

            if (shutdownConfig.EnableShutdown == false)
            {
                Logger.Warning("Shutdown blocked by configuration. The computer will not be shut down.");
                return;
            }

            Logger.InitiatingShutdown();

            // TODO: Check if there is a user-defined action that needs to be performed prior to
            // starting the shutdown sequence.

            Logger.InitiatingShutdown();

            string shutdownExePath =
                Environment.ExpandEnvironmentVariables("%system32%\\shutdown.exe");

            List<string> shutdownArgs = new List<string>();

            if (shutdownConfig.HibernateInsteadOfShutdown)
            {
                shutdownArgs.Add("/h");
            }
            else
            {
                if (shutdownConfig.AutoSignInNextBoot)
                {
                    shutdownArgs.Add("/sg");
                }
                else
                {
                    shutdownArgs.Add("/s");
                }
            }

            // Add a time to allow the user to shutdown anything
            shutdownArgs.Add($"/t {shutdownConfig.ShutdownDelayInSeconds}");

            // Add the force flag to ensure that apps aren't given extra delay to shutdown
            shutdownArgs.Add("/f");

            // Add a comment for tracking
            shutdownArgs.Add("/c \"Initiating shutdown due to power loss from UPS\"");

            // This is the reason code for 'Power Failure: Environment'
            shutdownArgs.Add("/d 6:12");

            string argString = string.Join(" ", shutdownArgs);

            Logger.Info(
                "Initiating shutdown command with path '{0}' and arguments '{1}'",
                shutdownExePath,
                argString);

            var psi = new ProcessStartInfo(shutdownExePath, argString);
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;

            // Process.Start(psi);
            Debugger.Break();
        }
    }
}
