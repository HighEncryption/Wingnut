namespace Wingnut.Service
{
    using System.ServiceProcess;

    using Wingnut.Core;
    using Wingnut.Tracing;

    public partial class ServiceCore : ServiceBase
    {
        public ServiceCore()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // Update the service state to Start Pending.
            ServiceStatus serviceStatus = new ServiceStatus
            {
                dwCurrentState = ServiceState.SERVICE_START_PENDING, 
                dwWaitHint = 10000
            };

            NativeMethods.SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            Logger.SetOutputType(Logger.OutputType.ETW);

            // Initialize the runtime (load configuration, etc.)
            ServiceRuntime.Instance.Initialize();

            // Start the WCF service and monitoring task
            ServiceRuntime.Instance.Start();

            // Update the service state to Running.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            NativeMethods.SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        protected override void OnStop()
        {
            // Update the service state to Stop Pending.
            ServiceStatus serviceStatus = new ServiceStatus
            {
                dwCurrentState = ServiceState.SERVICE_STOP_PENDING, 
                dwWaitHint = 10000
            };

            NativeMethods.SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            ServiceRuntime.Instance.Stop();

            // Update the service state to Stopped.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOPPED;
            NativeMethods.SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }
    }
}
