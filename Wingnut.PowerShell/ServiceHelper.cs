namespace Wingnut.PowerShell
{
    using System.ServiceModel;

    using Wingnut.Channels;
    using Wingnut.Data.Models;

    public class CallbackClient : IManagementCallback
    {
        public void SendCallbackMessage(string message)
        {
            throw new System.NotImplementedException();
        }

        public void UpsDeviceChanged(Ups ups)
        {
            throw new System.NotImplementedException();
        }
    }

    public class ServiceHelper
    {
        public IManagementService Channel { get; private set; }

        private ServiceHelper()
        {
        }

        public static ServiceHelper Create()
        {
            ServiceHelper helper = new ServiceHelper();

            CallbackClient callbackClient = new CallbackClient();
            InstanceContext context = new InstanceContext(callbackClient);

            DuplexChannelFactory<IManagementService> factory = new DuplexChannelFactory<IManagementService>(
                context,
                new NetNamedPipeBinding(),
                new EndpointAddress("net.pipe://localhost/Wingnut"));

            helper.Channel = factory.CreateChannel();

            return helper;
        }
    }
}