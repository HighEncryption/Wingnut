namespace Wingnut.Core
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Wingnut.Data;
    using Wingnut.Data.Configuration;
    using Wingnut.Data.Models;
    using Wingnut.Tracing;

    public class UpsContext
    {
        //private readonly ServerContext serverContext;

        internal readonly ServerConfiguration ServerConfiguration;

        private CancellationTokenSource cancellationTokenSource;
        private AsyncReaderWriterLock readerWriterLock;
        private AutoResetEvent upsStatusChangedEvent;

        public Server ServerState { get; }

        public ServerConnection Connection { get; }

        /// <summary>
        /// The DateTime when the notification was last sent that communication with the UPS has
        /// been lost.
        /// </summary>
        public DateTime? LastNoCommNotifyTime { get; set; }

        internal Task MonitoringTask { get; private set; }

        public string Name { get; }

        public string QualifiedName =>
            string.Format(
                "{0}@{1}:{2}", 
                this.Name, 
                this.ServerConfiguration.Address,
                this.ServerConfiguration.Port);

        public Ups State { get; set; }

        public UpsContext(
            ServerConfiguration serverConfiguration, 
            Server server,
            string name)
        {
            this.ServerConfiguration = serverConfiguration;
            this.ServerState = server;
            this.Name = name;

            this.Connection = new ServerConnection(server);
        }

        public static UpsContext CreateFromConfiguration(UpsConfiguration upsConfiguration)
        {
            Server server = Server.CreateFromConfiguration(
                upsConfiguration.ServerConfiguration);

            UpsContext upsContext = new UpsContext(
                upsConfiguration.ServerConfiguration,
                server, 
                upsConfiguration.DeviceName);

            return upsContext;
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
                async () => await this.MonitorUpsMain().ConfigureAwait(false),
                this.cancellationTokenSource.Token);
        }

        private async Task MonitorUpsMain()
        {
            try
            {
                while (!this.cancellationTokenSource.IsCancellationRequested)
                {
                    // Attempt to connect to the server if needed
                    if (this.ServerState.ConnectionStatus == ServerConnectionStatus.NotConnected ||
                        this.ServerState.ConnectionStatus == ServerConnectionStatus.LostConnection)
                    {
                        Logger.Debug(
                            "MonitorUpsMain[Ups={0}]: Attempting to connect. Current status is {1}",
                            this.QualifiedName,
                            this.ServerState.ConnectionStatus);

                        try
                        {
                            await this.Connection.ConnectAsync(this.cancellationTokenSource.Token)
                                .ConfigureAwait(false);

                            Logger.Info(
                                "MonitorUpsMain[Ups={0}]: Successfully connected",
                                this.QualifiedName);

                            // The connection was successful
                            this.ServerState.ConnectionStatus = ServerConnectionStatus.Connected;
                        }
                        catch (Exception exception)
                        {
                            Logger.Debug(
                                "MonitorUpsMain[Ups={0}]: Failed to connect. The error was: {1}",
                                this.QualifiedName,
                                exception.Message);
                        }
                    }

                    if (this.ServerState.ConnectionStatus == ServerConnectionStatus.Connected)
                    {
                        Logger.Debug(
                            "MonitorUpsMain[Ups={0}]: Calling UpdateStatusAsync()",
                            this.QualifiedName);

                        using (await this.readerWriterLock.ReaderLockAsync())
                        {
                            try
                            {
                                if (this.State == null)
                                {
                                    // We haven't yet pulled the device information from the server yet, so
                                    // do that now. Since this will be the first time pulling device state
                                    // from the server, we won't have any previous state to compare it to,
                                    // so don't bother calling the UpdateStatusAsync method below.
                                    Dictionary<string, string> upsVariables =
                                        await this.Connection
                                            .ListVarsAsync(this.Name, this.cancellationTokenSource.Token)
                                            .ConfigureAwait(false);

                                    this.State = new Ups(this.Name, this.ServerState, upsVariables);
                                }
                                else
                                {
                                    await this.UpdateStatusAsync(this.cancellationTokenSource.Token)
                                        .ConfigureAwait(true);

                                    Logger.Debug(
                                        "MonitorUpsMain[Ups={0}]: Successfully queried server",
                                        this.QualifiedName);
                                }
                            }
                            catch (NutCommunicationException communicationException)
                            {
                                Logger.Warning(
                                    "MonitorUpsMain[Ups={0}]: Lost connection to the server. The exception was: {1}",
                                    this.QualifiedName,
                                    communicationException.Message);

                                this.Disconnect(true, true);
                            }
                            catch (Exception exception)
                            {
                                Logger.Warning(
                                    "MonitorUpsMain[Ups={0}]: Failed to query server. The exception was: {1}",
                                    this.QualifiedName,
                                    exception.Message);
                            }
                        }
                    }

                    Logger.Debug(
                        "MonitorUpsMain[Ups={0}]: Delaying for {1} seconds",
                        this.QualifiedName,
                        ServiceRuntime.Instance.Configuration.ServiceConfiguration.PollFrequencyInSeconds);

                    await Task.Delay(
                            TimeSpan.FromSeconds(
                                ServiceRuntime.Instance.Configuration.ServiceConfiguration.PollFrequencyInSeconds),
                            this.cancellationTokenSource.Token)
                        .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
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
            bool statusChanged = false;

            try
            {
                // Get the current status of the device from the server
                Dictionary<string, string> deviceVars =
                    await this.Connection
                        .ListVarsAsync(this.Name, cancellationToken)
                        .ConfigureAwait(false);

                if (this.State == null)
                {
                    // We haven't yet pulled the device information from the server yet, so
                    // do that now. Since this will be the first time pulling device state
                    // from the server, we won't have any previous state to compare it to,
                    // so don't bother comparing state.
                    this.State = new Ups(this.Name, this.ServerState, deviceVars);

                    return true;
                }

                statusChanged = this.Update(deviceVars);

                this.State.LastPollTime = DateTime.Now;

                if (this.LastNoCommNotifyTime != null)
                {
                    ServiceRuntime.Instance.Notify(
                        this,
                        NotificationType.CommunicationRestored);

                    this.LastNoCommNotifyTime = null;
                }
            }
            catch (NutCommunicationException commEx)
            {
                if (this.LastNoCommNotifyTime == null)
                {
                    Logger.Error(
                        "Communication lost with UPS {0} on server {1}. The exception was: {2}",
                        this.Name,
                        this.ServerState.Name,
                        commEx);

                    ServiceRuntime.Instance.Notify(
                        this,
                        NotificationType.CommunicationLost);

                    this.LastNoCommNotifyTime = DateTime.Now;
                }
                else if (
                    this.LastNoCommNotifyTime.Value < DateTime.Now.AddSeconds(
                        0 - ServiceRuntime.Instance.Configuration.ServiceConfiguration.NoCommNotifyDelayInSeconds))
                {
                    ServiceRuntime.Instance.Notify(
                        this,
                        NotificationType.NoCommunication);

                    this.LastNoCommNotifyTime = DateTime.Now;
                }
            }

            Logger.Debug("ServerContext: Finished UpdateStatusAsync(). anyStatusChanged=" + statusChanged);
            return statusChanged;
        }

        public void StopMonitoring()
        {
            this.cancellationTokenSource.Cancel();
            this.MonitoringTask.Wait();
        }

        public bool Update(Dictionary<string, string> deviceVars)
        {
            DeviceStatusType initialStatus = this.State.Status;

            this.State.UpdateVariables(deviceVars);

            if (this.State.Status != initialStatus)
            {
                if (this.State.Status == DeviceStatusType.Online)
                {
                    ServiceRuntime.Instance.Notify(this, NotificationType.Online);
                }
                else if (this.State.Status == DeviceStatusType.OnBattery)
                {
                    ServiceRuntime.Instance.Notify(this, NotificationType.OnBattery);
                }
                else if (this.State.Status == DeviceStatusType.LowBattery)
                {
                    ServiceRuntime.Instance.Notify(this, NotificationType.LowBattery);
                }

                Logger.LogLevel logLevel = Logger.LogLevel.Info;
                DeviceSeverityType severity = Constants.Device.GetStatusSeverity(this.State.Status);

                if (severity == DeviceSeverityType.Error)
                {
                    logLevel = Logger.LogLevel.Error;
                }
                else if (severity == DeviceSeverityType.Warning)
                {
                    logLevel = Logger.LogLevel.Warning;
                }

                Logger.Log(
                    logLevel,
                    "Status of UPS {0} has changed from {1} to {2}",
                    this.Name,
                    initialStatus,
                    this.State.Status);

                return true;
            }

            return false;
        }

        public void Disconnect(bool suppressFailures, bool lostConnection)
        {
            Logger.Info("Closing connection to server {0}", this.ServerState.Name);

            // TODO: Think about this design. When do we issue a logout to the server?
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

            if (lostConnection)
            {
                this.ServerState.ConnectionStatus = ServerConnectionStatus.LostConnection;
            }
            else
            {
                this.ServerState.ConnectionStatus = ServerConnectionStatus.NotConnected;
            }
        }
    }
}