namespace Wingnut.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.ServiceModel.Configuration;
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
        private readonly ReaderWriterLockSlim monitoringSyncLock = new ReaderWriterLockSlim();

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
                        try
                        {
                            // Acquire exclusive access by acquiring the writer lock
                            this.monitoringSyncLock.EnterWriteLock();

                            // We have exclusive access to process status changes, so we need to be
                            // as efficient as possible in this area.
                            ProcessUpsStatusChanges();
                        }
                        finally
                        {
                            this.monitoringSyncLock.ExitWriteLock();
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
            throw new NotImplementedException();
        }

        //private async Task MonitorServerMain(ServerContext serverContext)
        //{
        //    while (!this.cancellationTokenSource.IsCancellationRequested)
        //    {
        //        if (serverContext.ServerState.ConnectionStatus == ServerConnectionStatus.NotConnected ||
        //            serverContext.ServerState.ConnectionStatus == ServerConnectionStatus.LostConnection)
        //        {
        //            Logger.Debug(
        //                "MonitorServerMain[Server={0}]: Attempting to connect. Current status is {1}",
        //                serverContext.ServerState.Name,
        //                serverContext.ServerState.ConnectionStatus);

        //            try
        //            {
        //                await serverContext.Connection.ConnectAsync(this.cancellationTokenSource.Token)
        //                    .ConfigureAwait(false);

        //                Logger.Info(
        //                    "MonitorServerMain[Server={0}]: Successfully connected",
        //                    serverContext.ServerState.Name);

        //                // The connection was successful
        //                serverContext.ServerState.ConnectionStatus = ServerConnectionStatus.Connected;
        //            }
        //            catch (Exception exception)
        //            {
        //                Logger.Debug(
        //                    "MonitorServerMain[Server={0}]: Failed to connect. The error was: {1}",
        //                    serverContext.ServerState.Name,
        //                    exception.Message);
        //            }
        //        }

        //        if (serverContext.ServerState.ConnectionStatus == ServerConnectionStatus.Connected)
        //        {
        //            Logger.Debug(
        //                "MonitorServerMain[Server={0}]: Calling UpdateStatusAsync()",
        //                serverContext.ServerState.Name);

        //            bool readLockAcquired = false;

        //            try
        //            {
        //                readLockAcquired =
        //                    this.monitoringSyncLock.TryEnterReadLock(readLockAcquisitionTimeout);

        //                if (!readLockAcquired)
        //                {
        //                    Logger.Warning(
        //                        "MonitorServerMain[Server={0}]: Failed to acquire read lock! Will try again later.",
        //                        serverContext.ServerState.Name);
        //                }
        //                else
        //                {
        //                    bool anyUpsStatusChanged = await serverContext
        //                        .UpdateStatusAsync(this.cancellationTokenSource.Token)
        //                        .ConfigureAwait(false);

        //                    if (anyUpsStatusChanged)
        //                    {
        //                        this.upsStatusChangedEvent.Set();
        //                    }

        //                    Logger.Debug(
        //                        "MonitorServerMain[Server={0}]: Successfully queried server",
        //                        serverContext.ServerState.Name);
        //                }
        //            }
        //            catch (NutCommunicationException communicationException)
        //            {
        //                Logger.Warning(
        //                    "MonitorServerMain[Server={0}]: Lost connection to the server. The exception was: {1}",
        //                    serverContext.ServerState.Name,
        //                    communicationException.Message);

        //                serverContext.Disconnect(true);

        //                serverContext.ServerState.ConnectionStatus =
        //                    ServerConnectionStatus.LostConnection;
        //            }
        //            catch (Exception exception)
        //            {
        //                Logger.Warning(
        //                    "MonitorServerMain[Server={0}]: Failed to query server. The exception was: {1}",
        //                    serverContext.ServerState.Name,
        //                    exception.Message);
        //            }
        //            finally
        //            {
        //                if (readLockAcquired)
        //                {
        //                    monitoringSyncLock.ExitReadLock();
        //                }
        //            }
        //        }

        //        Logger.Debug(
        //            "MonitorServerMain[Server={0}]: Delaying for {1} seconds",
        //            serverContext.ServerState.Name,
        //            serverContext.ServerState.PollFrequencyInSeconds);

        //        await Task.Delay(
        //                TimeSpan.FromSeconds(serverContext.ServerState.PollFrequencyInSeconds),
        //                this.cancellationTokenSource.Token)
        //            .ConfigureAwait(false);
        //    }
        //}

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
