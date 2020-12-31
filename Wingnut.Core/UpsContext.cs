namespace Wingnut.Core
{
    using System;
    using System.Collections.Generic;
    using System.Data.SQLite;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Wingnut.Channels;
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

        public MetricDatabase MetricDatabase { get; }

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

            this.MetricDatabase = GetMetricDatabase();
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

                        using (await this.upsMonitor.ReaderWriterLock.ReaderLockAsync())
                        {
                            try
                            {
                                await this.UpdateStatusAsync(this.cancellationTokenSource.Token)
                                    .ConfigureAwait(true);
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

                //Dictionary<string, string> deviceRw = 
                //    await this.Connection
                //        .ListRwAsync(this.Name, cancellationToken)
                //        .ConfigureAwait(false);

                //List<string> deviceCmd = 
                //    await this.Connection
                //        .ListCmdAsync(this.Name, cancellationToken)
                //        .ConfigureAwait(false);

                //Dictionary<string, string> deviceEnum =
                //    await this.Connection
                //        .ListEnumAsync(this.Name + " input.transfer.low", cancellationToken)
                //        .ConfigureAwait(false);

                //Dictionary<string, string> deviceRange =
                //    await this.Connection
                //        .ListRangeAsync(this.Name + " input.transfer.low", cancellationToken)
                //        .ConfigureAwait(false);

                Logger.Debug(
                    "UpdateStatusAsync[Ups={0}]: Successfully queried server",
                    this.QualifiedName);

                var metricMeasurements = this.MetricDatabase.UpdateMetrics(deviceVars);

                if (this.State == null)
                {
                    // We haven't yet pulled the device information from the server yet, so
                    // do that now. Since this will be the first time pulling device state
                    // from the server, we won't have any previous state to compare it to,
                    // so don't bother comparing state.
                    this.State = Ups.Create(this.Name, this.ServerState, deviceVars);

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
                    this.State.Update(deviceVars);

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

#pragma warning disable 4014
                    Task.Run(() =>
                    {
                        foreach (IManagementCallback callbackChannel in 
                            ServiceRuntime.Instance.ClientCallbackChannels)
                        {
                            try
                            {
                                callbackChannel.UpsDeviceChanged(this.State, metricMeasurements.ToArray());
                            }
                            catch (Exception e)
                            {
                                Logger.Error("Caught exception while updating device. " + e.Message);
                                ServiceRuntime.Instance.ClientCallbackChannels.Remove(callbackChannel);
                                break;
                            }
                        }
                    }, cancellationToken);
#pragma warning restore 4014
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

        private string GenerateMetricDatabaseID()
        {
            string source =
                string.Format(
                    "{0}-{1}-{2}",
                    this.UpsConfiguration.ServerConfiguration.Address.ToUpperInvariant(),
                    this.UpsConfiguration.ServerConfiguration.Port,
                    this.UpsConfiguration.DeviceName.ToUpperInvariant());

            using (MD5 md5 = new MD5CryptoServiceProvider())
            {
                byte[] hashInput = Encoding.Unicode.GetBytes(source);
                byte[] hashOutput = md5.ComputeHash(hashInput);

                return BitConverter
                    .ToString(hashOutput)
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();
            }
        }

        private MetricDatabase GetMetricDatabase()
        {
            string databaseFilename =
                string.Format("{0}.db", GenerateMetricDatabaseID());

            string databaseFilePath = Path.Combine(
                ServiceRuntime.Instance.AppDataPath,
                databaseFilename);

            SQLiteConnectionStringBuilder builder = new SQLiteConnectionStringBuilder
            {
                DataSource = databaseFilePath
            };

            //builder.Pooling = true;

            MetricDatabase database = new MetricDatabase()
            {
                ConnectionString = builder.ConnectionString
            };

            using (var connection = database.CreateConnection())
            {
                using (var cmd = new SQLiteCommand(connection))
                {
                    cmd.CommandText =
                        @"CREATE TABLE IF NOT EXISTS vars (id INT PRIMARY KEY, variableName VARCHAR, dataType INT, tableName VARCHAR)";

                    cmd.ExecuteNonQuery();
                }
                connection.Close();
            }

            return database;
        }
    }

    public class UpsStatusChangeData
    {
        public UpsContext UpsContext { get; set; }

        public Ups PreviousState { get; set; }

        public Exception Exception { get; set; }
    }

    public class MetricDatabase
    {
        private readonly VariableMetric[] variableMetrics = 
        {
            new VariableMetric("battery.charge"), 
            new VariableMetric("battery.current"), 
            new VariableMetric("battery.runtime"), 
            new VariableMetric("battery.voltage"), 
            new VariableMetric("input.frequency"), 
            new VariableMetric("input.voltage"), 
            new VariableMetric("output.current"), 
            new VariableMetric("output.frequency"), 
            new VariableMetric("output.voltage"), 
            new VariableMetric("ups.temperature"), 
        };

        public string ConnectionString { get; internal set; }

        public SQLiteConnection CreateConnection()
        {
            var connection = new SQLiteConnection(this.ConnectionString);
            return connection.OpenAndReturn();
        }

        public List<MetricMeasurement> UpdateMetrics(Dictionary<string, string> deviceVars)
        {
            SQLiteConnection connection;
            List<MetricMeasurement> metricMeasurements = new List<MetricMeasurement>();

            using (connection = this.CreateConnection())
            {
                foreach (KeyValuePair<string, string> deviceVar in deviceVars)
                {
                    if (!TryGetVariableMetric(deviceVar.Key, out VariableMetric metric))
                    {
                        continue;
                    }

                    if (!metric.Initialized)
                    {
                        InitializeMetric(connection, metric);
                    }

                    MetricMeasurement metricMeasurement = new MetricMeasurement()
                    {
                        VariableName = metric.VariableName,
                        Timestamp = DateTime.UtcNow,
                        Value = double.Parse(deviceVar.Value)
                    };

                    InsertMetricRaw(connection, metric, metricMeasurement);

                    InsertMetric1Minute(connection, metric, metricMeasurement);

                    metricMeasurements.Add(metricMeasurement);
                }

                connection.Close();
            }

            return metricMeasurements;
        }

        private static void InsertMetricRaw(
            SQLiteConnection connection, 
            VariableMetric metric,
            MetricMeasurement metricMeasurement)
        {
            if (!metric.Initialized)
            {
                throw new Exception("Metric is not initialized!");
            }



            SQLiteCommand cmd;
            using (cmd = new SQLiteCommand(connection))
            {
                cmd.CommandText = $"INSERT INTO [{metric.TableName}_raw] (timestamp, value) VALUES (@timestamp, @value)";
                cmd.Parameters.AddWithValue("@timestamp", metricMeasurement.Timestamp.ToEpochSeconds());
                cmd.Parameters.AddWithValue("@value", metricMeasurement.Value);

                cmd.ExecuteNonQuery();
            }
        }

        private static void InsertMetric1Minute(
            SQLiteConnection connection, 
            VariableMetric metric,
            MetricMeasurement metricMeasurement)
        {
            if (!metric.Initialized)
            {
                throw new Exception("Metric is not initialized!");
            }

            var timestamp = new DateTime(
                metricMeasurement.Timestamp.Year,
                metricMeasurement.Timestamp.Month, 
                metricMeasurement.Timestamp.Day,
                metricMeasurement.Timestamp.Hour, 
                metricMeasurement.Timestamp.Minute, 
                0);

            double? newValue = null;
            int valueCount = 0;

            using (var cmd = new SQLiteCommand(connection))
            {
                cmd.CommandText = $"SELECT * FROM [{metric.TableName}_1m] WHERE [timestamp] = @ts";
                cmd.Parameters.AddWithValue("@ts", timestamp.ToEpochSeconds());

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (string.IsNullOrWhiteSpace(metric.TableName))
                        {
                            throw new Exception("TableName read from metric row is empty");
                        }

                        double value = (double) reader["value"];
                        valueCount = (int) reader["count"];

                        newValue = ((value * valueCount) + metricMeasurement.Value) / (valueCount + 1);
                        break;
                    }
                }
            }

            if (newValue != null)
            {
                using (var cmd = new SQLiteCommand(connection))
                {
                    cmd.CommandText = $"UPDATE [{metric.TableName}_1m] SET [value] = @value, [count] = @count WHERE [timestamp] = @ts";
                    cmd.Parameters.AddWithValue("@value", newValue.Value);
                    cmd.Parameters.AddWithValue("@count", valueCount + 1);
                    cmd.Parameters.AddWithValue("@ts", timestamp.ToEpochSeconds());

                    cmd.ExecuteNonQuery();
                }

                return;
            }

            using (var cmd = new SQLiteCommand(connection))
            {
                cmd.CommandText = $"INSERT INTO [{metric.TableName}_1m] (timestamp, value, count) VALUES (@ts, @value, @count)";
                cmd.Parameters.AddWithValue("@ts", timestamp.ToEpochSeconds());
                cmd.Parameters.AddWithValue("@value", metricMeasurement.Value);
                cmd.Parameters.AddWithValue("@count", 1);

                cmd.ExecuteNonQuery();
            }
        }

        private bool TryGetVariableMetric(string variableName, out VariableMetric metric)
        {
            metric =
                variableMetrics.FirstOrDefault(
                    m => string.Equals(
                        m.VariableName,
                        variableName,
                        StringComparison.Ordinal));

            return metric != null;
        }

        private void InitializeMetric(
            SQLiteConnection connection, 
            VariableMetric metric)
        {
            using (var cmd = new SQLiteCommand(connection))
            {
                cmd.CommandText = @"SELECT * FROM [vars] WHERE [variableName] = '@name'";
                cmd.Parameters.AddWithValue("@name", metric.VariableName);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // There is a row for this variable
                        metric.TableName = Convert.ToString(reader["tableName"]);

                        if (string.IsNullOrWhiteSpace(metric.TableName))
                        {
                            throw new Exception("TableName read from metric row is empty");
                        }

                        metric.Initialized = true;
                        return;
                    }
                }
            }

            // The metric has not been initialized, so do it now
            using (var cmd = new SQLiteCommand(connection))
            {
                cmd.CommandText = "INSERT INTO [vars] (variableName, dataType, tableName) VALUES (@varName, @dataType, @tableName)";
                cmd.Parameters.AddWithValue("@varName", metric.VariableName);
                cmd.Parameters.AddWithValue("@dataType", (int)metric.DataType);
                cmd.Parameters.AddWithValue("@tableName", metric.TableName);

                cmd.ExecuteNonQuery();
            }

            using (var cmd = new SQLiteCommand(connection))
            {
                cmd.CommandText = string.Format(
                    @"CREATE TABLE IF NOT EXISTS [{0}_raw] (timestamp INT PRIMARY KEY, value DOUBLE)",
                    metric.TableName);

                cmd.ExecuteNonQuery();
            }

            using (var cmd = new SQLiteCommand(connection))
            {
                cmd.CommandText = string.Format(
                    @"CREATE TABLE IF NOT EXISTS [{0}_1m] (timestamp INT PRIMARY KEY, value DOUBLE, count INT)",
                    metric.TableName);

                cmd.ExecuteNonQuery();
            }

            metric.Initialized = true;
        }
    }

    internal enum VariableDataType
    {
        Integer = 0,
        Double = 1
    }

    internal class VariableMetric
    {
        public string VariableName { get; set; }

        public VariableDataType DataType { get; set; }

        public string TableName { get; set; }

        public bool Initialized { get; set; }

        public VariableMetric(
            string variableName,
            VariableDataType dataType = VariableDataType.Double,
            string tableName = null)
        {
            this.VariableName = variableName;
            this.DataType = dataType;

            if (tableName == null)
            {
                tableName = variableName.Replace('.', '_');
            }

            this.TableName = tableName;
        }
    }
}