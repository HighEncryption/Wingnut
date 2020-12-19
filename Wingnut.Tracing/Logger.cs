namespace Wingnut.Tracing
{
    using System;
    using System.Globalization;

    using JetBrains.Annotations;

    public static class Logger
    {
        public enum OutputType
        {
            ETW,
            Console
        }

        public enum LogLevel
        {
            Critical,
            Error,
            Warning,
            Info,
            Debug
        }

        public enum EtwLog
        {
            Service,
            App
        }

        public static LogLevel OutputLogLevel = LogLevel.Info;

        public static EtwLog EtwLogDestination = EtwLog.Service;

        private static OutputType outputType = OutputType.ETW;

        private static bool isOutputTypeSet = false;

        public static void SetOutputType(OutputType type)
        {
            if (isOutputTypeSet)
            {
                throw new InvalidOperationException("Output type is already set");
            }

            Logger.outputType = type;

            isOutputTypeSet = true;
        }

        [StringFormatMethod("message")]
        public static void Critical(string message, params object[] args)
        {
            Log(LogLevel.Critical, message, args);
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

            WingnutServiceEventSource.Log.LogError(
                string.Format("{0}\n\n{1}", formattedMessage, exception));
        }

        [StringFormatMethod("message")]
        public static void Log(LogLevel level, string message, params object[] args)
        {
            if (level > OutputLogLevel)
            {
                return;
            }

            if (outputType == OutputType.Console)
            {
                WriteToConsole(level, message, args);
                return;
            }

            switch (EtwLogDestination)
            {
                case EtwLog.Service:
                    LogEtwService(level, message, args);
                    break;
                case EtwLog.App:
                    LogEtwApp(level, message, args);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

        }

        private static void LogEtwService(LogLevel level, string message, params object[] args)
        {
            switch (level)
            {
                case LogLevel.Critical:
                    WingnutServiceEventSource.Log.LogCritical(string.Format(message, args));
                    break;
                case LogLevel.Debug:
                    WingnutServiceEventSource.Log.LogDebug(string.Format(message, args));
                    break;
                case LogLevel.Info:
                    WingnutServiceEventSource.Log.LogInformational(string.Format(message, args));
                    break;
                case LogLevel.Warning:
                    WingnutServiceEventSource.Log.LogWarning(string.Format(message, args));
                    break;
                case LogLevel.Error:
                    WingnutServiceEventSource.Log.LogError(string.Format(message, args));
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private static void LogEtwApp(LogLevel level, string message, params object[] args)
        {
            switch (level)
            {
                case LogLevel.Critical:
                    WingnutAppEventSource.Log.LogCritical(string.Format(message, args));
                    break;
                case LogLevel.Debug:
                    WingnutAppEventSource.Log.LogDebug(string.Format(message, args));
                    break;
                case LogLevel.Info:
                    WingnutAppEventSource.Log.LogInformational(string.Format(message, args));
                    break;
                case LogLevel.Warning:
                    WingnutAppEventSource.Log.LogWarning(string.Format(message, args));
                    break;
                case LogLevel.Error:
                    WingnutAppEventSource.Log.LogError(string.Format(message, args));
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        #region Service ETW event

        public static void UpsOnline(string upsName, string serverName)
        {
            WingnutServiceEventSource.Log.UpsOnline(upsName, serverName);
        }

        public static void UpsOnBattery(string upsName, string serverName)
        {
            WingnutServiceEventSource.Log.UpsOnBattery(upsName, serverName);
        }

        public static void UpsLowBattery(string upsName, string serverName)
        {
            WingnutServiceEventSource.Log.UpsLowBattery(upsName, serverName);
        }

        public static void UpsReplaceBattery(string upsName, string serverName, DateTime? lastReplaceDate)
        {
            string replacementDate = "(unknown)";

            if (lastReplaceDate != null)
            {
                replacementDate = lastReplaceDate.Value.ToString("d", CultureInfo.CurrentCulture);
            }

            WingnutServiceEventSource.Log.UpsBatteryNeedsReplaced(upsName, serverName, replacementDate);
        }

        public static void CommunicationLost(string upsName, string serverName, string error)
        {
            WingnutServiceEventSource.Log.CommunicationLost(upsName, serverName, error);
        }

        public static void NoCommunication(string upsName, string serverName, string error)
        {
            WingnutServiceEventSource.Log.NoCommunication(upsName, serverName, error);
        }

        public static void CommunicationRestored(string upsName, string serverName)
        {
            WingnutServiceEventSource.Log.CommunicationRestored(upsName, serverName);
        }

        public static void ConnectedToServer(string serverName)
        {
            WingnutServiceEventSource.Log.ConnectedToServer(serverName);
        }

        public static void FailedToQueryServer(string serverName, string error)
        {
            WingnutServiceEventSource.Log.FailedToQueryServer(serverName, error);
        }

        public static void PowerValueBelowThreshold(int powerValue, int threshold)
        {
            WingnutServiceEventSource.Log.PowerValueBelowThreshold(powerValue, threshold);
        }

        public static void InitiatingShutdown()
        {
            WingnutServiceEventSource.Log.InitiatingShutdown();
        }

        public static void NotificationScriptNotFound(string scriptPath)
        {
            WingnutServiceEventSource.Log.NotificationScriptNotFound(scriptPath);
        }

        public static void ServiceStarting(string version)
        {
            WingnutServiceEventSource.Log.ServiceStarting(version);
        }

        public static void ServiceStarted()
        {
            WingnutServiceEventSource.Log.ServiceStarted();
        }

        #endregion

        private static void WriteToConsole(LogLevel level, string message, object[] args)
        {
            try
            {
                string strLevel;

                switch (level)
                {
                    case LogLevel.Critical:
                        Console.ForegroundColor = ConsoleColor.Red;
                        strLevel = "CRT";
                        break;
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

                Console.WriteLine(
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