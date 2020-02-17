namespace Wingnut.Tracing
{
    using System.Diagnostics.Tracing;

    [EventSource(Name = "Wingnut-App")]
    public sealed class WingnutAppEventSource : EventSource
    {
        public static WingnutAppEventSource Log = new WingnutAppEventSource();

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

        public class EventIDs
        {
            public const int LogCritical = 1;
            public const int LogError = 2;
            public const int LogWarning = 3;
            public const int LogInformational = 4;
            public const int LogVerbose = 5;
            public const int LogDebug = 6;
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