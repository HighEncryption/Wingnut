namespace Wingnut.Tracing
{
    using System;

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