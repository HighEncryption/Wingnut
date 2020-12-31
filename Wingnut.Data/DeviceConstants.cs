namespace Wingnut.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Wingnut.Tracing;

    public enum SSLUsage
    {
        Undefined = 0,
        Disabled = 1,
        Optional = 2,
        Required = 3
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
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

    public sealed class DeviceStatusDisplayNameAttribute : Attribute
    {
        public string DisplayName { get; set; }

        public DeviceStatusDisplayNameAttribute(string displayName)
        {
            this.DisplayName = displayName;
        }
    }

    public sealed class DevicePropertyAttribute : Attribute
    {
        public string PropertyName { get; set; }

        public Type ConverterType { get; set; }

        public DevicePropertyAttribute(string propertyName)
        {
            this.PropertyName = propertyName;
        }
    }

    public class DeviceStatusConverter : IDeviceValueConverter
    {
        public object Convert(object source)
        {
            return Constants.Device.ParseStatusString(source as string);
        }
    }

    public interface IDeviceValueConverter
    {
        object Convert(object source);
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
        [DeviceStatusDisplayName("Off")]
        Off = 0x0001,

        [DeviceStatusIdentifier("OL")]
        [DeviceStatusIdentifier("ONLINE")]
        [DeviceStatusSeverity(DeviceSeverityType.OK)]
        [DeviceStatusDisplayName("Online")]
        Online = 0x0002,

        [DeviceStatusIdentifier("OB")]
        [DeviceStatusSeverity(DeviceSeverityType.Error)]
        [DeviceStatusDisplayName("On Battery")]
        OnBattery = 0x0004,

        [DeviceStatusIdentifier("LB")]
        [DeviceStatusIdentifier("LOWBATT")]
        [DeviceStatusSeverity(DeviceSeverityType.Error)]
        [DeviceStatusDisplayName("Low Battery")]
        LowBattery = 0x0008,

        [DeviceStatusIdentifier("RB")]
        [DeviceStatusSeverity(DeviceSeverityType.Error)]
        [DeviceStatusDisplayName("Replace Battery")]
        ReplaceBattery = 0x0010,

        [DeviceStatusIdentifier("OVER")]
        [DeviceStatusSeverity(DeviceSeverityType.Error)]
        [DeviceStatusDisplayName("Overload")]
        Overload = 0x0020,

        [DeviceStatusIdentifier("TRIM")]
        [DeviceStatusSeverity(DeviceSeverityType.Warning)]
        [DeviceStatusDisplayName("Voltage Trim")]
        VoltageTrim = 0x0040,

        [DeviceStatusIdentifier("BOOST")]
        [DeviceStatusSeverity(DeviceSeverityType.Warning)]
        [DeviceStatusDisplayName("Voltage Boost")]
        VoltageBoost = 0x0080,

        [DeviceStatusIdentifier("CAL")]
        [DeviceStatusSeverity(DeviceSeverityType.Warning)]
        [DeviceStatusDisplayName("Calibrating")]
        Calibration = 0x0100,

        [DeviceStatusIdentifier("BYPASS")]
        [DeviceStatusSeverity(DeviceSeverityType.Error)]
        [DeviceStatusDisplayName("Bypass")]
        Bypass = 0x0200,

        [DeviceStatusIdentifier("CHRG")]
        [DeviceStatusSeverity(DeviceSeverityType.OK)]
        [DeviceStatusDisplayName("Charging")]
        Charging = 0x0400,

        [DeviceStatusIdentifier("DISCHRG")]
        [DeviceStatusSeverity(DeviceSeverityType.OK)]
        [DeviceStatusDisplayName("Discharging")]
        Discharging = 0x0800
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
            internal class DeviceStatusInfo
            {
                public string[] Identifiers;
                public string DisplayName;
                public DeviceStatusType StatusType;
                public DeviceSeverityType Severity;
            }

            internal static readonly List<DeviceStatusInfo> statusInfos;

            static Device()
            {
                FieldInfo[] enumValues =
                    typeof(DeviceStatusType).GetFields(BindingFlags.Public | BindingFlags.Static);

                statusInfos = new List<DeviceStatusInfo>();

                foreach (FieldInfo enumValue in enumValues)
                {
                    List<DeviceStatusIdentifierAttribute> idAttributes =
                        enumValue.GetCustomAttributes()
                            .OfType<DeviceStatusIdentifierAttribute>()
                            .ToList();

                    if (!idAttributes.Any())
                    {
                        continue;
                    }

                    DeviceStatusDisplayNameAttribute displayNameAttribute =
                        enumValue.GetCustomAttributes()
                            .OfType<DeviceStatusDisplayNameAttribute>()
                            .First();

                    DeviceStatusSeverityAttribute severityAttribute =
                        enumValue.GetCustomAttributes()
                            .OfType<DeviceStatusSeverityAttribute>()
                            .First();

                    statusInfos.Add(
                        new DeviceStatusInfo()
                        {
                            Identifiers = idAttributes.Select(a => a.Name).ToArray(),
                            DisplayName = displayNameAttribute?.DisplayName,
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
                            s => s.Identifiers.Contains(token, StringComparer.OrdinalIgnoreCase));
//                            s => string.Equals(s.Identifier, token, StringComparison.OrdinalIgnoreCase));

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
                DeviceSeverityType severity = DeviceSeverityType.OK;

                foreach (DeviceStatusInfo deviceStatusInfo in statusInfos)
                {
                    if (status.HasFlag(deviceStatusInfo.StatusType) &&
                        deviceStatusInfo.Severity > severity)
                    {
                        severity = deviceStatusInfo.Severity;
                    }
                }

                return severity;
            }

        }
    }

    public static class DeviceStatusTypeExtensions
    {
        public static List<string> GetStatusDisplayName(this DeviceStatusType status)
        {
            List<string> result = new List<string>();
            foreach (Constants.Device.DeviceStatusInfo info in Constants.Device.statusInfos)
            {
                if (status.HasFlag(info.StatusType))
                {
                    result.Add(info.DisplayName);
                }
            }

            return result;
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
