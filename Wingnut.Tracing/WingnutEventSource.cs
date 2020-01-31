namespace Wingnut.Tracing
{
    using System.Diagnostics.Tracing;

    public class WingnutEventSource : EventSource
    {
        public static WingnutEventSource Log = new WingnutEventSource();

        #region Generic Events

        [Event(
            EventIDs.LogCritical,
            Channel = EventChannel.Debug,
            Level = EventLevel.Critical,
            Task = Tasks.General,
            Opcode = Opcodes.Critical,
            Keywords = EventKeywords.None,
            Message = "{0}")]
        public void LogCritical(string message)
        {
            this.WriteEvent(EventIDs.LogCritical, message);
        }

        [Event(
            EventIDs.LogError,
            Channel = EventChannel.Debug,
            Level = EventLevel.Error,
            Task = Tasks.General,
            Opcode = Opcodes.Error,
            Keywords = EventKeywords.None,
            Message = "{0}")]
        public void LogError(string message)
        {
            this.WriteEvent(EventIDs.LogError, message);
        }

        [Event(
            EventIDs.LogWarning,
            Channel = EventChannel.Debug,
            Task = Tasks.General,
            Opcode = Opcodes.Warning,
            Level = EventLevel.Warning,
            Keywords = EventKeywords.None,
            Message = "{0}")]
        public void LogWarning(string message)
        {
            this.WriteEvent(EventIDs.LogWarning, message);
        }

        [Event(
            EventIDs.LogInformational,
            Channel = EventChannel.Debug,
            Level = EventLevel.Informational,
            Opcode = Opcodes.Informational,
            Task = Tasks.General,
            Keywords = EventKeywords.None,
            Message = "{0}")]
        public void LogInformational(string message)
        {
            this.WriteEvent(EventIDs.LogInformational, message);
        }

        [Event(
            EventIDs.LogVerbose,
            Channel = EventChannel.Debug,
            Level = EventLevel.Verbose,
            Opcode = Opcodes.Verbose,
            Task = Tasks.General,
            Keywords = EventKeywords.None,
            Message = "{0}")]
        public void LogVerbose(string message)
        {
            this.WriteEvent(EventIDs.LogVerbose, message);
        }

        [Event(
            EventIDs.LogDebug,
            Channel = EventChannel.Debug,
            Level = EventLevel.Verbose,
            Opcode = Opcodes.Debug,
            Task = Tasks.General,
            Message = "{0}")]
        public void LogDebug(string message)
        {
            this.WriteEvent(EventIDs.LogDebug, message);
        }

        #endregion

        [Event(
            EventIDs.UpsOnline,
            Channel = EventChannel.Operational,
            Level = EventLevel.Informational,
            Message = "The UPS {0} on server {1} is now online")]
        public void UpsOnline(string upsName, string serverName)
        {
            this.WriteEvent(EventIDs.UpsOnline, upsName, serverName);
        }

        [Event(
            EventIDs.UpsOnBattery,
            Channel = EventChannel.Operational,
            Level = EventLevel.Warning,
            Message = "The UPS {0} on server {1} is running on battery")]
        public void UpsOnBattery(string upsName, string serverName)
        {
            this.WriteEvent(EventIDs.UpsOnBattery, upsName, serverName);
        }

        [Event(
            EventIDs.UpsLowBattery,
            Channel = EventChannel.Operational,
            Level = EventLevel.Warning,
            Message = "The UPS {0} on server {1} has a low battery")]
        public void UpsLowBattery(string upsName, string serverName)
        {
            this.WriteEvent(EventIDs.UpsLowBattery, upsName, serverName);
        }

        [Event(
            EventIDs.UpsBatteryNeedsReplaced,
            Channel = EventChannel.Operational,
            Level = EventLevel.Error,
            Message = "The battery for UPS {0} on server {1} needs to be replaced. The previous install date is {1}")]
        public void UpsBatteryNeedsReplaced(string upsName, string serverName, string lastReplaceDate)
        {
            this.WriteEvent(EventIDs.UpsBatteryNeedsReplaced, upsName, serverName, lastReplaceDate);
        }

        [Event(
            EventIDs.CommunicationLost,
            Channel = EventChannel.Operational,
            Level = EventLevel.Error,
            Message = "Communication lost to UPS {0} on server {1}. The error was: {2}")]
        public void CommunicationLost(string upsName, string serverName, string error)
        {
            this.WriteEvent(EventIDs.CommunicationLost, upsName, serverName, error);
        }

        [Event(
            EventIDs.CommunicationRestored,
            Channel = EventChannel.Operational,
            Level = EventLevel.Informational,
            Message = "Communication restored to UPS {0} on server {1}")]
        public void CommunicationRestored(string upsName, string serverName)
        {
            this.WriteEvent(EventIDs.CommunicationRestored, upsName, serverName);
        }

        [Event(
            EventIDs.ConnectedToServer,
            Channel = EventChannel.Operational,
            Level = EventLevel.Informational,
            Message = "Successfully connected to server {0}")]
        public void ConnectedToServer(string serverName)
        {
            this.WriteEvent(EventIDs.ConnectedToServer, serverName);
        }

        [Event(
            EventIDs.FailedToQueryServer,
            Channel = EventChannel.Operational,
            Level = EventLevel.Informational,
            Message = "Failed to query server {0}. The error was: {1}")]
        public void FailedToQueryServer(string serverName, string error)
        {
            this.WriteEvent(EventIDs.FailedToQueryServer, serverName, error);
        }

        [Event(
            EventIDs.PowerValueBelowThreshold,
            Channel = EventChannel.Operational,
            Level = EventLevel.Error,
            Message = "Power value for the computer is {0}, which is below the threshold of {1}")]
        public void PowerValueBelowThreshold(int powerValue, int threshold)
        {
            this.WriteEvent(EventIDs.CommunicationLost, powerValue, threshold);
        }

        [Event(
            EventIDs.InitiatingShutdown,
            Channel = EventChannel.Operational,
            Level = EventLevel.Error,
            Message = "The shutdown process is being initiated")]
        public void InitiatingShutdown()
        {
            this.WriteEvent(EventIDs.CommunicationLost);
        }

        public class EventIDs
        {
            public const int LogCritical = 1;
            public const int LogError = 2;
            public const int LogWarning = 3;
            public const int LogInformational = 4;
            public const int LogVerbose = 5;
            public const int LogDebug = 6;

            public const int UpsOnline = 10;
            public const int UpsOnBattery = 11;
            public const int UpsLowBattery = 12;
            public const int UpsBatteryNeedsReplaced = 13;
            public const int CommunicationLost = 14;
            public const int CommunicationRestored = 15;
            public const int ConnectedToServer = 16;
            public const int FailedToQueryServer = 17;
            public const int PowerValueBelowThreshold = 18;
            public const int InitiatingShutdown = 19;
        }

        public class Tasks
        {
            public const EventTask General = (EventTask)0x01;
        }

        public class Opcodes
        {
            public const EventOpcode Critical = (EventOpcode)0x0b;
            public const EventOpcode Error = (EventOpcode)0x0c;
            public const EventOpcode Warning = (EventOpcode)0x0d;
            public const EventOpcode Informational = (EventOpcode)0x0e;
            public const EventOpcode Verbose = (EventOpcode)0x0f;
            public const EventOpcode Debug = (EventOpcode)0x10;
        }
    }
}
