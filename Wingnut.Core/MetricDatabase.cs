namespace Wingnut.Core
{
    using System;
    using System.Collections.Generic;
    using System.Data.SQLite;
    using System.Linq;

    using Wingnut.Data;
    using Wingnut.Data.Models;

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
                this.variableMetrics.FirstOrDefault(
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
}