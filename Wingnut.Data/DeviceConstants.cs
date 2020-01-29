namespace Wingnut.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

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
    public enum DeviceStatusType
    {
        Undefined,

        [DeviceStatusIdentifier("OFF")]
        [DeviceStatusSeverity(DeviceSeverityType.Warning)]
        Off,

        [DeviceStatusIdentifier("OL")]
        [DeviceStatusSeverity(DeviceSeverityType.OK)]
        Online,

        [DeviceStatusIdentifier("OB")]
        [DeviceStatusSeverity(DeviceSeverityType.Error)]
        OnBattery,

        [DeviceStatusIdentifier("LB")]
        [DeviceStatusSeverity(DeviceSeverityType.Error)]
        LowBattery,

        [DeviceStatusIdentifier("RB")]
        [DeviceStatusSeverity(DeviceSeverityType.Error)]
        ReplaceBattery,

        [DeviceStatusIdentifier("OVER")]
        [DeviceStatusSeverity(DeviceSeverityType.Error)]
        Overload,

        [DeviceStatusIdentifier("TRIM")]
        [DeviceStatusSeverity(DeviceSeverityType.Warning)]
        VoltageTrim,

        [DeviceStatusIdentifier("BOOST")]
        [DeviceStatusSeverity(DeviceSeverityType.Warning)]
        VoltageBoost,

        [DeviceStatusIdentifier("CAL")]
        [DeviceStatusSeverity(DeviceSeverityType.Warning)]
        Calibration,

        [DeviceStatusIdentifier("BYPASS")]
        [DeviceStatusSeverity(DeviceSeverityType.Error)]
        Bypass
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

            public static DeviceStatusType GetStatusType(string identifier)
            {
                DeviceStatusInfo statusInfo =
                    statusInfos.FirstOrDefault(
                        s => String.Equals(s.Identifier, identifier, StringComparison.OrdinalIgnoreCase));

                if (statusInfo == null)
                {
                    throw new Exception($"The identifier {identifier} is not defined");
                }

                return statusInfo.StatusType;
            }

            public static DeviceSeverityType GetStatusSeverity(string identifier)
            {
                DeviceStatusInfo statusInfo =
                    statusInfos.FirstOrDefault(
                        s => String.Equals(s.Identifier, identifier, StringComparison.OrdinalIgnoreCase));

                if (statusInfo == null)
                {
                    throw new Exception($"The identifier {identifier} is not defined");
                }

                return statusInfo.Severity;
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

}
