namespace Wingnut.Core
{
    using System;
    using System.Collections.Generic;
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
                    foreach (ServerContext serverContext in ServiceRuntime.Instance.ServerContexts)
                    {
                        if (serverContext.MonitoringTask == null)
                        {
                            serverContext.StartMonitoring(
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
            //throw new NotImplementedException();
            Logger.Info("Processing status change");
        }

        private void InitializeFromConfiguration()
        {
            foreach (ServerConfiguration serverConfiguration
                in ServiceRuntime.Instance.Configuration.Servers)
            {
                serverConfiguration.ValidateProperties();

                Logger.Info($"Loading server {serverConfiguration.Name} from configuration");

                if (ServiceRuntime.Instance.ServerContexts.Any(
                    s => string.Equals(
                        s.ServerState.Name,
                        serverConfiguration.Name,
                        StringComparison.OrdinalIgnoreCase)))
                {
                    throw new WingnutException("The server already exists");
                }

                Server server = Server.CreateFromConfiguration(serverConfiguration);
                ServerContext serverContext = new ServerContext(server, serverConfiguration);

                // Add to the server list without attempting to connect, since this will be
                // handled in the monitoring loop.
                ServiceRuntime.Instance.ServerContexts.Add(serverContext);

                Logger.Info("Server loaded successfully");
            }

            Logger.Info(
                "Finished loading {0} servers from configuration",
                ServiceRuntime.Instance.Configuration.Servers.Count);
        }
    }

}
