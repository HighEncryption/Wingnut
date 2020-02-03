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
        public UpsConfiguration UpsConfiguration { get; }

        private UpsMonitor upsMonitor;
        private CancellationTokenSource cancellationTokenSource;

        public Server ServerState { get; }

        public ServerConnection Connection { get; }

        /// <summary>
        /// The DateTime when the notification was last sent that communication with the UPS has
        /// been lost.
        /// </summary>
        public DateTime? LastNoCommNotifyTime { get; set; }

        public DateTime? LastReplaceBatteryWarnTime { get; set; }

        internal Task MonitoringTask { get; private set; }

        public string Name { get; }

        public string QualifiedName =>
            string.Format(
                "{0}@{1}:{2}", 
                this.Name, 
                this.UpsConfiguration.ServerConfiguration.Address,
                this.UpsConfiguration.ServerConfiguration.Port);

        public Ups State { get; set; }

        public UpsContext(
            UpsConfiguration upsConfiguration, 
            Server server)
        {
            this.UpsConfiguration = upsConfiguration;
            this.ServerState = server;

            this.Name = upsConfiguration.DeviceName;

            this.Connection = new ServerConnection(server);
        }

        public void StartMonitoring(
            UpsMonitor monitor,
            CancellationToken monitoringCancellationToken)
        {
            this.upsMonitor = monitor;

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

                            Logger.Debug(
                                "MonitorUpsMain[Ups={0}]: Successfully connected",
                                this.QualifiedName);

                            Logger.ConnectedToServer(this.ServerState.Name);

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

                        using (await this.upsMonitor.readerWriterLock.ReaderLockAsync())
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
                                }
                            }
                            catch (Exception exception)
                            {
                                Logger.FailedToQueryServer(this.QualifiedName, exception.Message);

                                Logger.Warning(
                                    "MonitorUpsMain[Ups={0}]: Failed to query server. The exception was: {1}",
                                    this.QualifiedName,
                                    exception.Message);
                            }
                        }
                    }

                    int pollDelay = ServiceRuntime.Instance.Configuration
                        .ServiceConfiguration.PollFrequencyInSeconds;

                    bool pollUrgent = 
                        this.State?.Status.HasFlag(DeviceStatusType.OnBattery) == true;

                    if (pollUrgent)
                    {
                        pollDelay = ServiceRuntime.Instance.Configuration
                            .ServiceConfiguration.PollFrequencyUrgentInSeconds;
                    }

                    Logger.Debug(
                        "MonitorUpsMain[Ups={0}]: Delaying for {1} seconds{2}",
                        this.QualifiedName,
                        pollDelay,
                        pollUrgent ? " (URGENT)" : string.Empty);

                    await Task.Delay(
                            TimeSpan.FromSeconds(pollDelay),
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
        private async Task UpdateStatusAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Get the current status of the device from the server
                Dictionary<string, string> deviceVars = 
                    await this.Connection
                        .ListVarsAsync(this.Name, cancellationToken)
                        .ConfigureAwait(false);

                Logger.Debug(
                    "UpdateStatusAsync[Ups={0}]: Successfully queried server",
                    this.QualifiedName);

                if (this.State == null)
                {
                    // We haven't yet pulled the device information from the server yet, so
                    // do that now. Since this will be the first time pulling device state
                    // from the server, we won't have any previous state to compare it to,
                    // so don't bother comparing state.
                    this.State = new Ups(this.Name, this.ServerState, deviceVars);

                    // ReSharper disable once MethodSupportsCancellation
                    this.upsMonitor.Changes.Add(
                        new UpsStatusChangeData
                        {
                            // Pass null as the previous state to indicate that this is the first time
                            // receiving state information for this device
                            PreviousState = null,
                            UpsContext = this
                        });
                }
                else
                {
                    // Create a copy of the current state in case we need it below
                    var previousState = this.State.Clone();

                    // Update the state object is with the new variables from the server
                    this.State.UpdateVariables(deviceVars);

                    // The status has changed, so queue a status change notification
                    if (previousState.Status != this.State.Status)
                    {
                        // ReSharper disable once MethodSupportsCancellation
                        this.upsMonitor.Changes.Add(
                            new UpsStatusChangeData
                            {
                                // Create a copy of the device state to pass to UpsMonitor
                                PreviousState = previousState,
                                UpsContext = this
                            });
                    }
                }

                // We successfully queried the server, so update the property for this
                this.State.LastPollTime = DateTime.Now;
            }
            catch (NutCommunicationException commEx)
            {
                Logger.Debug(
                    "UpdateStatusAsync[Ups={0}]: Caught exception querying server. {1}",
                    this.QualifiedName,
                    commEx);

                this.Disconnect(true, true);

                // We failed to communicate with the server, so raise a change notification
                // ReSharper disable once MethodSupportsCancellation
                this.upsMonitor.Changes.Add(
                    new UpsStatusChangeData
                    {
                        // Create a copy of the device state to pass to UpsMonitor
                        PreviousState = this.State.Clone(),
                        UpsContext = this,
                        Exception = commEx
                    });
            }

            Logger.Debug(
                "UpdateStatusAsync[Ups={0}]: Finished UpdateStatusAsync()",
                this.QualifiedName);
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
        private async Task UpdateStatusAsync2(CancellationToken cancellationToken)
        {
            try
            {
                // Get the current status of the device from the server
                Dictionary<string, string> deviceVars =
                    await this.Connection
                        .ListVarsAsync(this.Name, cancellationToken)
                        .ConfigureAwait(false);

                Logger.Debug(
                    "MonitorUpsMain[Ups={0}]: Successfully queried server",
                    this.QualifiedName);

                if (this.State == null)
                {
                    // We haven't yet pulled the device information from the server yet, so
                    // do that now. Since this will be the first time pulling device state
                    // from the server, we won't have any previous state to compare it to,
                    // so don't bother comparing state.
                    this.State = new Ups(this.Name, this.ServerState, deviceVars);

                    // ReSharper disable once MethodSupportsCancellation
                    this.upsMonitor.Changes.Add(
                        new UpsStatusChangeData
                        {
                            // Pass null as the previous state to indicate that this is the first time
                            // receiving state information for this device
                            PreviousState = null,
                            UpsContext = this
                        });
                }
                else
                {
                    // Create a copy of the current state in case we need it below
                    var previousState = this.State.Clone();

                    // Update the state object is with the new variables from the server
                    this.State.UpdateVariables(deviceVars);

                    // The status has changed, so queue a status change notification
                    if (previousState.Status != this.State.Status)
                    {
                        // ReSharper disable once MethodSupportsCancellation
                        this.upsMonitor.Changes.Add(
                            new UpsStatusChangeData
                            {
                                // Create a copy of the device state to pass to UpsMonitor
                                PreviousState = this.State.Clone(),
                                UpsContext = this
                            });
                    }
                }

                // We successfully queried the server, so update the property for this
                this.State.LastPollTime = DateTime.Now;

                // Was communication previous lost and has not been re-established?
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
                    Logger.UpsOnline(
                        this.Name,
                        this.ServerState.Name);

                    Logger.Debug(
                        "MonitorUpsMain[Ups={0}]: Communication lost with UPS. The exception was: {1}",
                        this.QualifiedName,
                        commEx.Message);

                    ServiceRuntime.Instance.Notify(
                        this,
                        NotificationType.CommunicationLost);

                    this.LastNoCommNotifyTime = DateTime.Now;
                }
                else if (
                    this.LastNoCommNotifyTime.Value.OlderThan(
                        TimeSpan.FromSeconds(
                            ServiceRuntime.Instance.Configuration.ServiceConfiguration.NoCommNotifyDelayInSeconds)))
                {
                    ServiceRuntime.Instance.Notify(
                        this,
                        NotificationType.NoCommunication);

                    this.LastNoCommNotifyTime = DateTime.Now;
                }

                this.Disconnect(true, true);
            }

            Logger.Debug(
                "MonitorUpsMain[Ups={0}]: Finished UpdateStatusAsync()",
                this.QualifiedName);
        }

        public void StopMonitoring()
        {
            this.cancellationTokenSource.Cancel();
            this.Disconnect(true, false);
            this.MonitoringTask.Wait(1000);
        }

        public void Disconnect(bool suppressFailures, bool lostConnection)
        {
            Logger.Warning("Closing connection to server {0}", this.ServerState.Name);

            // TODO: Think about this design. When do we issue a logout to the server?
            // TODO: Should be retry for a while?
            try
            {
                this.Connection.Disconnect();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }

            // Set the connection status according to whether the connection was intentionally
            // closed or was unintentionally lost.
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

    public class UpsStatusChangeData
    {
        public UpsContext UpsContext { get; set; }

        public Ups PreviousState { get; set; }

        public Exception Exception { get; set; }
    }
}