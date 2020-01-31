namespace Wingnut.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Configuration;
    using System.Reflection;

    using Wingnut.Tracing;

    public enum SSLUsage
    {
        Undefined = 0,
        Disabled = 1,
        Optional = 2,
        Required = 3
    }

    public sealed class DeviceStatusIdentifierAttribute : Attribute
    {
        public string Name { get; set; }

        public DeviceStatusIdentifierAttribute(string name)
        {
            this.Name = name;
        }
    }

    public sealed class DeviceStatusSeverityAttribute : Attribute
    {
        public DeviceSeverityType Severity { get; set; }

        public DeviceStatusSeverityAttribute(DeviceSeverityType severity)
        {
            this.Severity = severity;
        }
    }

    /// <summary>
    /// The status of the device
    /// </summary>
    /// <remarks>
    /// The values here are derived from clients/status.h
    /// </remarks>
    [Flags]
    public enum DeviceStatusType
    {
        Undefined,

        [DeviceStatusIdentifier("OFF")]
        [DeviceStatusSeverity(DeviceSeverityType.Warning)]
        Off = 0x0001,

        [DeviceStatusIdentifier("OL")]
        [DeviceStatusSeverity(DeviceSeverityType.OK)]
        Online = 0x0002,

        [DeviceStatusIdentifier("OB")]
        [DeviceStatusSeverity(DeviceSeverityType.Error)]
        OnBattery = 0x0004,

        [DeviceStatusIdentifier("LB")]
        [DeviceStatusSeverity(DeviceSeverityType.Error)]
        LowBattery = 0x0008,

        [DeviceStatusIdentifier("RB")]
        [DeviceStatusSeverity(DeviceSeverityType.Error)]
        ReplaceBattery = 0x0010,

        [DeviceStatusIdentifier("OVER")]
        [DeviceStatusSeverity(DeviceSeverityType.Error)]
        Overload = 0x0020,

        [DeviceStatusIdentifier("TRIM")]
        [DeviceStatusSeverity(DeviceSeverityType.Warning)]
        VoltageTrim = 0x0040,

        [DeviceStatusIdentifier("BOOST")]
        [DeviceStatusSeverity(DeviceSeverityType.Warning)]
        VoltageBoost = 0x0080,

        [DeviceStatusIdentifier("CAL")]
        [DeviceStatusSeverity(DeviceSeverityType.Warning)]
        Calibration = 0x0100,

        [DeviceStatusIdentifier("BYPASS")]
        [DeviceStatusSeverity(DeviceSeverityType.Error)]
        Bypass = 0x0200
    }

    public enum DeviceSeverityType
    {
        OK = 0,
        Warning = 1,
        Error = 2
    }

    public enum NotificationType
    {
        Online,
        OnBattery,
        LowBattery,
        ForcedShutdown,
        CommunicationRestored,
        CommunicationLost,
        Shutdown,
        ReplaceBattery,
        NoCommunication
    }

    public static class Constants
    {
        public const int DefaultPollFrequencyInSeconds = 15;

        public const int DefaultPortNumber = 3493;

        public const int DefaultNumPowerSupplies = 1;

        public static class Device
        {
            private class DeviceStatusInfo
            {
                public string Identifier;
                public DeviceStatusType StatusType;
                public DeviceSeverityType Severity;
            }

            private static readonly List<DeviceStatusInfo> statusInfos;

            static Device()
            {
                FieldInfo[] enumValues =
                    typeof(DeviceStatusType).GetFields(BindingFlags.Public | BindingFlags.Static);

                statusInfos = new List<DeviceStatusInfo>();

                foreach (FieldInfo enumValue in enumValues)
                {
                    DeviceStatusIdentifierAttribute idAttribute =
                        enumValue.GetCustomAttributes(typeof(DeviceStatusIdentifierAttribute))
                            .OfType<DeviceStatusIdentifierAttribute>()
                            .FirstOrDefault();

                    if (idAttribute == null)
                    {
                        continue;
                    }

                    DeviceStatusSeverityAttribute severityAttribute =
                        enumValue.GetCustomAttributes(typeof(DeviceStatusSeverityAttribute))
                            .OfType<DeviceStatusSeverityAttribute>()
                            .First();

                    statusInfos.Add(
                        new DeviceStatusInfo()
                        {
                            Identifier = idAttribute.Name,
                            Severity = severityAttribute.Severity,
                            StatusType = (DeviceStatusType)enumValue.GetRawConstantValue()
                        });
                }
            }

            public static Logger.LogLevel DeviceSeverityToLogLevel(DeviceSeverityType severity)
            {
                switch (severity)
                {
                    case DeviceSeverityType.OK:
                        return Logger.LogLevel.Info;
                    case DeviceSeverityType.Warning:
                        return Logger.LogLevel.Warning;
                    case DeviceSeverityType.Error:
                        return Logger.LogLevel.Error;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(severity), severity, null);
                }
            }

            public static DeviceStatusType ParseStatusString(string status)
            {
                DeviceStatusType statusToReturn = DeviceStatusType.Undefined;

                if (string.IsNullOrWhiteSpace(status))
                {
                    // We didn't get back any status information from the device, like
                    // indicating that the UPS is gone.
                    return statusToReturn;
                }

                var tokens = status.Split(' ');
                foreach (string token in tokens)
                {
                    DeviceStatusInfo statusInfo =
                        statusInfos.FirstOrDefault(
                            s => string.Equals(s.Identifier, token, StringComparison.OrdinalIgnoreCase));

                    if (statusInfo == null)
                    {
                        throw new Exception($"The status {token} is not defined");
                    }

                    statusToReturn |= statusInfo.StatusType;
                }

                return statusToReturn;
            }

            public static DeviceSeverityType GetStatusSeverity(DeviceStatusType status)
            {
                DeviceStatusInfo statusInfo = statusInfos.FirstOrDefault(s => s.StatusType == status);

                if (statusInfo == null)
                {
                    throw new Exception($"The status type {status} is not defined");
                }

                return statusInfo.Severity;
            }
        }
    }

    public static class EnumExtensions
    {
        private static readonly string[] undefinedFlagNames = { "undefined" };

        public static IEnumerable<string> GetSetFlagNames<TEnum>(object value)
        {
            Type enumType = typeof(TEnum);
            if (!enumType.IsEnum)
            {
                throw new InvalidOperationException("Type " + enumType.FullName + " is not a Enum type.");
            }

            List<string> set = new List<string>();
            foreach (object val in Enum.GetValues(enumType))
            {
                if ((Convert.ToUInt64(value) & Convert.ToUInt64(val)) != 0)
                {
                    string name = Enum.GetName(enumType, val);
                    if (!undefinedFlagNames.All(n => n.Equals(name, StringComparison.OrdinalIgnoreCase)))
                    {
                        set.Add(name);
                    }
                }
            }

            return set;
        }
    }
}
