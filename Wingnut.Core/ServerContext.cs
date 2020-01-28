namespace Wingnut.Core
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Wingnut.Data;
    using Wingnut.Data.Configuration;
    using Wingnut.Data.Models;
    using Wingnut.Tracing;


    public class ServerContext
    {
        internal readonly ServerConfiguration Configuration;

        private CancellationTokenSource cancellationTokenSource;
        private AsyncReaderWriterLock readerWriterLock;
        private AutoResetEvent upsStatusChangedEvent;
        public Task MonitoringTask { get; private set; }

        public Server ServerState { get; }

        public List<UpsContext> UpsContexts { get; }

        public ServerConnection Connection { get; }

        public DateTime LastPollTime { get; private set; }

        public ServerContext(Server server, ServerConfiguration configuration)
        {
            this.UpsContexts = new List<UpsContext>();
            this.Configuration = configuration;
            this.ServerState = server;

            this.Connection = new ServerConnection(this);
        }

        public async Task<List<DeviceDefinition>> GetUpsDefinitionsAsync()
        {
            Dictionary<string, string> response = 
                await this.Connection.ListUpsAsync(CancellationToken.None)
                    .ConfigureAwait(false);

            List<DeviceDefinition> upsList = new List<DeviceDefinition>();

            foreach (string upsName in response.Keys)
            {
                upsList.Add(
                    new DeviceDefinition()
                    {
                        Name = upsName,
                        Description = response[upsName],
                        DeviceType = DeviceType.UPS
                    });

            }

            return upsList;
        }

        /// <summary>
        /// Update...
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <remarks>
        /// Note: The caller must hold a reader lock from UpsMonitor prior to calling
        /// this method
        /// </remarks>
        private async Task<bool> UpdateStatusAsync(CancellationToken cancellationToken)
        {
            Logger.Debug("ServerContext: Starting UpdateStatusAsync()");

            ServerConfiguration serverConfiguration =
                ServiceRuntime.Instance.Configuration.Servers.FirstOrDefault(s => s.Name == this.ServerState.Name);

            if (serverConfiguration == null)
            {
                throw new WingnutException(
                    $"Failed to find server configuration with name {this.ServerState.Name}");
            }

            List<DeviceDefinition> deviceDefinitions = null;
            bool anyStatusChanged = false;

            // Add any new devices
            foreach (UpsConfiguration upsConfig in serverConfiguration.Upses)
            {
                UpsContext upsContext = this.UpsContexts.FirstOrDefault(u => u.Name == upsConfig.Name);

                // Check if a device is defined in the configuration but has not had its
                // corresponding context created yet.
                if (upsContext != null)
                {
                    continue;
                }

                // Get the list of devices defined on the server one time.
                if (deviceDefinitions == null)
                {
                    deviceDefinitions =
                        await this.GetUpsDefinitionsAsync().ConfigureAwait(false);
                }

                DeviceDefinition deviceDefinition =
                    deviceDefinitions.FirstOrDefault(d => d.Name == upsConfig.Name);

                if (deviceDefinition == null)
                {
                    throw new WingnutException(
                        $"A device with name '{upsConfig.Name}' was not found the server.");
                }

                Dictionary<string, string> deviceVars =
                    await this.Connection.ListVarsAsync(upsConfig.Name, cancellationToken)
                        .ConfigureAwait(false);

                upsContext = new UpsContext(
                    new Ups(upsConfig.Name, deviceVars)
                    {
                        LastPollTime = DateTime.UtcNow
                    });

                Logger.Info("Created new UpsContext for device '{0}'", upsConfig.Name);

                this.UpsContexts.Add(upsContext);
                anyStatusChanged = true;
            }

            foreach (UpsContext upsContext in this.UpsContexts)
            {
                TimeSpan timeSinceLastPoll = DateTime.UtcNow - upsContext.State.LastPollTime;
                if (timeSinceLastPoll < TimeSpan.FromSeconds(this.ServerState.PollFrequencyInSeconds))
                {
                    // Not enough time has passed since the last poll
                    continue;
                }

                Dictionary<string, string> deviceVars =
                    await this.Connection.ListVarsAsync(upsContext.Name, cancellationToken)
                        .ConfigureAwait(false);

                if (upsContext.Update(deviceVars))
                {
                    anyStatusChanged = true;
                }
            }

            this.LastPollTime = DateTime.Now;

            Logger.Debug("ServerContext: Finished UpdateStatusAsync(). anyStatusChanged=" + anyStatusChanged);
            return anyStatusChanged;
        }

        public void StartMonitoring(
            AsyncReaderWriterLock monitoringReaderWriterLock,
            AutoResetEvent upsStatusChanged,
            CancellationToken monitoringCancellationToken)
        {
            this.readerWriterLock = monitoringReaderWriterLock;
            this.upsStatusChangedEvent = upsStatusChanged;

            this.cancellationTokenSource = 
                CancellationTokenSource.CreateLinkedTokenSource(
                    monitoringCancellationToken);

            this.MonitoringTask = Task.Run(
                async () => await this.MonitorServerMain().ConfigureAwait(false),
                this.cancellationTokenSource.Token);
        }

        private readonly AutoResetEvent pollNowEvent = new AutoResetEvent(false);

        public void PollNow()
        {
            this.pollNowEvent.Set();
        }

        private async Task MonitorServerMain()
        {
            try
            {
                while (!this.cancellationTokenSource.IsCancellationRequested)
                {
                    if (this.ServerState.ConnectionStatus == ServerConnectionStatus.NotConnected ||
                        this.ServerState.ConnectionStatus == ServerConnectionStatus.LostConnection)
                    {
                        Logger.Debug(
                            "MonitorServerMain[Server={0}]: Attempting to connect. Current status is {1}",
                            this.ServerState.Name,
                            this.ServerState.ConnectionStatus);

                        try
                        {
                            await this.Connection.ConnectAsync(this.cancellationTokenSource.Token)
                                .ConfigureAwait(false);

                            Logger.Info(
                                "MonitorServerMain[Server={0}]: Successfully connected",
                                this.ServerState.Name);

                            // The connection was successful
                            this.ServerState.ConnectionStatus = ServerConnectionStatus.Connected;
                        }
                        catch (Exception exception)
                        {
                            Logger.Debug(
                                "MonitorServerMain[Server={0}]: Failed to connect. The error was: {1}",
                                this.ServerState.Name,
                                exception.Message);
                        }
                    }

                    if (this.ServerState.ConnectionStatus == ServerConnectionStatus.Connected)
                    {
                        Logger.Debug(
                            "MonitorServerMain[Server={0}]: Calling UpdateStatusAsync()",
                            this.ServerState.Name);

                        using (await this.readerWriterLock.ReaderLockAsync())
                        {
                            try
                            {
                                bool anyUpsStatusChanged = await this
                                    .UpdateStatusAsync(this.cancellationTokenSource.Token)
                                    .ConfigureAwait(true);

                                if (anyUpsStatusChanged)
                                {
                                    Logger.Debug("Setting StatusChangedEvent");
                                    this.upsStatusChangedEvent.Set();
                                }

                                Logger.Debug(
                                    "MonitorServerMain[Server={0}]: Successfully queried server",
                                    this.ServerState.Name);
                            }
                            catch (NutCommunicationException communicationException)
                            {
                                Logger.Warning(
                                    "MonitorServerMain[Server={0}]: Lost connection to the server. The exception was: {1}",
                                    this.ServerState.Name,
                                    communicationException.Message);

                                this.Disconnect(true);

                                this.ServerState.ConnectionStatus =
                                    ServerConnectionStatus.LostConnection;
                            }
                            catch (Exception exception)
                            {
                                Logger.Warning(
                                    "MonitorServerMain[Server={0}]: Failed to query server. The exception was: {1}",
                                    this.ServerState.Name,
                                    exception.Message);
                            }
                        }
                    }

                    Logger.Debug(
                        "MonitorServerMain[Server={0}]: Delaying for {1} seconds",
                        this.ServerState.Name,
                        this.ServerState.PollFrequencyInSeconds);

                    Stopwatch delayStopwatch = Stopwatch.StartNew();

                    while (true)
                    {
                        if (this.pollNowEvent.WaitOne(0))
                        {
                            // We need to poll now
                            Logger.Debug("Polling triggered by PollNow() call");
                            break;
                        }

                        if (delayStopwatch.Elapsed.TotalSeconds > this.ServerState.PollFrequencyInSeconds)
                        {
                            // We have waited enough time between polls
                            Logger.Debug("Polling triggered by expired timer");
                            break;
                        }

                        await Task.Delay(250, this.cancellationTokenSource.Token).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        public void Disconnect(bool suppressFailures)
        {
            Logger.Info("Closing connection to server {0}", this.ServerState.Name);

            // TODO: This about this design. When do we issue a logout to the server?
            // TODO: Should be retry for a while?
            try
            {
                //this.Connection.Close();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
        }

        public void StopMonitoring()
        {
            this.cancellationTokenSource.Cancel();
            this.MonitoringTask.Wait();

            //try
            //{
            //    this.MonitoringTask.Wait();
            //}
            //catch (Exception exception)
            //{
            //    // Suppress any exceptions due to task cancellation
            //    Logger.Debug(
            //        "Suppressing exception due to task cancellation: {0}", 
            //        exception.Message);
            //}
        }
    }
}