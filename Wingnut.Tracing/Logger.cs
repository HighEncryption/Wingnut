namespace Wingnut.Tracing
{
    using System;
    using System.Globalization;

    using JetBrains.Annotations;

    public class Logger
    {
        public enum OutputType
        {
            ETW,
            Console
        }

        public enum LogLevel
        {
            Error,
            Warning,
            Info,
            Debug
        }

        private static OutputType outputType = OutputType.ETW;

        private static bool isOutputTypeSet = false;

        public static void SetOutputType(OutputType outputType)
        {
            if (isOutputTypeSet)
            {
                throw new InvalidOperationException("Output type is already set");
            }

            Logger.outputType = outputType;

            isOutputTypeSet = true;
        }

        [StringFormatMethod("message")]
        public static void Error(string message, params object[] args)
        {
            Log(LogLevel.Error, message, args);
        }

        [StringFormatMethod("message")]
        public static void Warning(string message, params object[] args)
        {
            Log(LogLevel.Warning, message, args);
        }

        [StringFormatMethod("message")]
        public static void Info(string message, params object[] args)
        {
            Log(LogLevel.Info, message, args);
        }

        [StringFormatMethod("message")]
        public static void Debug(string message, params object[] args)
        {
            Log(LogLevel.Debug, message, args);
        }

        [StringFormatMethod("message")]
        public static void LogException(Exception exception, string message, params object[] args)
        {
            string formattedMessage = string.Format(message, args);

            if (outputType == OutputType.Console)
            {
                WriteToConsole(LogLevel.Error, "{0}\n\n{1}", new object[] { formattedMessage, exception });
                return;
            }

            WingnutEventSource.Log.LogError(
                string.Format("{0}\n\n{1}", formattedMessage, exception));
        }

        [StringFormatMethod("message")]
        public static void Log(LogLevel level, string message, params object[] args)
        {
            if (outputType == OutputType.Console)
            {
                WriteToConsole(level, message, args);
                return;
            }

            switch (level)
            {
                case LogLevel.Debug:
                    WingnutEventSource.Log.LogDebug(string.Format(message, args));
                    break;
                case LogLevel.Info:
                    WingnutEventSource.Log.LogInformational(string.Format(message, args));
                    break;
                case LogLevel.Warning:
                    WingnutEventSource.Log.LogWarning(string.Format(message, args));
                    break;
                case LogLevel.Error:
                    WingnutEventSource.Log.LogError(string.Format(message, args));
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public static void UpsOnline(string upsName, string serverName)
        {
            WingnutEventSource.Log.UpsOnline(upsName, serverName);
        }

        public static void UpsOnBattery(string upsName, string serverName)
        {
            WingnutEventSource.Log.UpsOnBattery(upsName, serverName);
        }

        public static void UpsLowBattery(string upsName, string serverName)
        {
            WingnutEventSource.Log.UpsLowBattery(upsName, serverName);
        }

        public static void UpsReplaceBattery(string upsName, string serverName, DateTime? lastReplaceDate)
        {
            string replacementDate = "(unknown)";

            if (lastReplaceDate != null)
            {
                replacementDate = lastReplaceDate.Value.ToString("d", CultureInfo.CurrentCulture);
            }

            WingnutEventSource.Log.UpsBatteryNeedsReplaced(upsName, serverName, replacementDate);
        }

        public static void CommunicationLost(string upsName, string serverName, string error)
        {
            WingnutEventSource.Log.CommunicationLost(upsName, serverName, error);
        }

        public static void CommunicationRestored(string upsName, string serverName)
        {
            WingnutEventSource.Log.CommunicationRestored(upsName, serverName);
        }

        public static void ConnectedToServer(string serverName)
        {
            WingnutEventSource.Log.ConnectedToServer(serverName);
        }

        public static void FailedToQueryServer(string serverName, string error)
        {
            WingnutEventSource.Log.FailedToQueryServer(serverName, error);
        }

        public static void PowerValueBelowThreshold(int powerValue, int threshold)
        {
            WingnutEventSource.Log.PowerValueBelowThreshold(powerValue, threshold);
        }

        public static void InitiatingShutdown()
        {
            WingnutEventSource.Log.InitiatingShutdown();
        }

        public static void NotificationScriptNotFound(string scriptPath)
        {
            WingnutEventSource.Log.NotificationScriptNotFound(scriptPath);
        }

        private static void WriteToConsole(LogLevel level, string message, object[] args)
        {
            try
            {
                string strLevel;

                switch (level)
                {
                    case LogLevel.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                        strLevel = "ERR";
                        break;
                    case LogLevel.Warning:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        strLevel = "WRN";
                        break;
                    case LogLevel.Info:
                        Console.ResetColor();
                        strLevel = "INF";
                        break;
                    case LogLevel.Debug:
                        strLevel = "DBG";
                        break;
                    default:
                        strLevel = "???";
                        Console.ResetColor();
                        break;
                }

                System.Diagnostics.Debug.WriteLine(
                    "[{0:s}] {1} {2}", DateTime.Now,
                    strLevel,
                    string.Format(message, args));
            }
            finally
            {
                Console.ResetColor();
            }
        }
    }
}