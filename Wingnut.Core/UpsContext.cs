namespace Wingnut.Core
{
    using System.Collections.Generic;

    using Wingnut.Data;
    using Wingnut.Data.Models;
    using Wingnut.Tracing;

    public class UpsContext
    {
        public string Name => this.State.Name;

        public Ups State { get; }

        public UpsContext(Ups state)
        {
            this.State = state;
        }

        public bool Update(Dictionary<string, string> deviceVars)
        {
            DeviceStatusType initialStatus = this.State.Status;

            this.State.UpdateVariables(deviceVars);

            if (this.State.Status != initialStatus)
            {
                Logger.LogLevel logLevel = Logger.LogLevel.Info;
                DeviceSeverityType severity = DeviceConstants.GetStatusSeverity(this.State.Status);

                if (severity == DeviceSeverityType.Error)
                {
                    logLevel = Logger.LogLevel.Error;
                }
                else if (severity == DeviceSeverityType.Warning)
                {
                    logLevel = Logger.LogLevel.Warning;
                }

                Logger.Log(
                    logLevel,
                    "Status of UPS {0} has changed from {1} to {2}",
                    this.Name,
                    initialStatus,
                    this.State.Status);

                return true;
            }

            return false;
        }
    }
}