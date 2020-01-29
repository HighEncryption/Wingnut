namespace Wingnut.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Wingnut.Data;
    using Wingnut.Data.Configuration;
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

        private List<Task> upsMonitoringTasks;

        public MonitorStatus Status { get; private set; }

        public void Start()
        {
            this.cancellationTokenSource = new CancellationTokenSource();
            this.upsMonitoringTasks = new List<Task>();

            monitorTask = Task.Run(async () => await this.UpsMonitorMain().ConfigureAwait(false));
        }

        public void Stop()
        {
            this.cancellationTokenSource.Cancel();
            this.monitorTask.Wait();
        }

        private readonly AutoResetEvent upsStatusChangedEvent = new AutoResetEvent(false);

        /// <summary>
        /// The synchronization object used gate access to process status changes for UPS devices.
        /// </summary>
        /// <remarks>
        /// The reader/writer locking pattern is used here so that updates can be made to UPSes
        /// concurrently (by acquiring a reader lock), however processing those updates should be
        /// done while other updates cannot be made (by acquiring the writer lock).
        /// </remarks>
        //private readonly ReaderWriterLockSlim monitoringSyncLock = new ReaderWriterLockSlim();
        private readonly AsyncReaderWriterLock monitoringSyncLock = new AsyncReaderWriterLock();

        private async Task UpsMonitorMain()
        {
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
                            upsContext.MonitoringTask.IsCompleted)
                        {
                            upsContext.StartMonitoring(
                                this.monitoringSyncLock,
                                this.upsStatusChangedEvent,
                                this.cancellationTokenSource.Token);
                        }
                    }

                    if (this.upsStatusChangedEvent.WaitOne(0))
                    {
                        Logger.Debug("UpsMonitorMain: Received signal to process device status change");

                        // The status of a UPS has changed, so wait until we have exclusive access
                        // to all UPSes then process the change
                        using (await this.monitoringSyncLock.WriterLockAsync())
                        {
                            // We have exclusive access to process status changes, so we need to be
                            // as efficient as possible in this area.
                            ProcessUpsStatusChanges();
                        }
                    }

                    await Task.Delay(100).ConfigureAwait(false);
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

        private void ProcessUpsStatusChanges()
        {
            Logger.Info("Processing status change");
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

                    Logger.Debug("Exception during upsContext load. {0}", exception);
                }

                if (ServiceRuntime.Instance.UpsContexts.Any(
                    s => string.Equals(
                        s.QualifiedName, 
                        upsConfiguration.GetQualifiedName())))
                {
                    throw new WingnutException("The device already exists");
                }

                UpsContext upsContext = UpsContext.CreateFromConfiguration(upsConfiguration);

                ServiceRuntime.Instance.UpsContexts.Add(upsContext);

                Logger.Info($"Ups '{upsContext.Name}' successfully loaded from configuration");
            }

            Logger.Info(
                "Finished loading {0} devices from configuration",
                ServiceRuntime.Instance.Configuration.UpsConfigurations.Count);
        }
    }
}
