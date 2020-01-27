namespace Wingnut.PowerShell
{
    using System.Collections.Generic;
    using System.Management.Automation;
    using System.ServiceModel;

    using Wingnut.Channels;
    using Wingnut.Data;
    using Wingnut.Data.Models;

    [Cmdlet(VerbsCommon.Get, "NutServer")]
    public class GetNutServer : PSCmdlet
    {
        [Parameter]
        public string Name { get; set; }

        protected override void ProcessRecord()
        {
            var callbackClient = new CallbackClient();
            var context = new InstanceContext(callbackClient);

            var factory = new DuplexChannelFactory<IManagementService>(
                context,
                new NetNamedPipeBinding(),
                new EndpointAddress("net.pipe://localhost/Wingnut"));

            var service = factory.CreateChannel();

            List<Server> servers = service.GetServers(this.Name);

            this.WriteObject(servers, true);
        }
    }

    [Cmdlet(VerbsCommon.Add, "NutServer")]
    public class AddNutServer : PSCmdlet
    {
        public const int DefaultPollFrequencyInSeconds = 15;
        public const int DefaultPortNumber = 3493;

        [Parameter(Mandatory = true)]
        public string Address { get; set; }

        [Parameter]
        public int Port { get; set; } = DefaultPortNumber;

        [Parameter(Mandatory = true)]
        public PSCredential Credential { get; set; }

        [Parameter]
        public int PollFrequencyInSeconds { get; set; } = DefaultPollFrequencyInSeconds;

        [Parameter]
        public SSLUsage UseSSL { get; set; }

        [Parameter]
        public SwitchParameter Force { get; set; }

        protected override void ProcessRecord()
        {
            var callbackClient = new CallbackClient();
            var context = new InstanceContext(callbackClient);

            var factory = new DuplexChannelFactory<IManagementService>(
                context,
                new NetNamedPipeBinding(),
                new EndpointAddress("net.pipe://localhost/Wingnut"));

            var service = factory.CreateChannel();

            Server server = new Server()
            {
                Address = this.Address,
                Port = this.Port,
                Username = this.Credential.UserName,
                PollFrequencyInSeconds = this.PollFrequencyInSeconds,
                UseSSL = this.UseSSL,
            };

            server = service.AddServer(
                server, 
                this.Credential.Password.GetDecrypted(), 
                this.Force.ToBool());

            this.WriteObject(server);
        }
    }

    [Cmdlet(VerbsCommon.Remove, "NutServer")]
    public class RemoveNutServer : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public string Name { get; set; }

        protected override void ProcessRecord()
        {
            var callbackClient = new CallbackClient();
            var context = new InstanceContext(callbackClient);

            var factory = new DuplexChannelFactory<IManagementService>(
                context,
                new NetNamedPipeBinding(),
                new EndpointAddress("net.pipe://localhost/Wingnut"));

            var service = factory.CreateChannel();

            service.DeleteServer(this.Name);
        }
    }

    [Cmdlet(VerbsCommon.Get, "NutUpsFromServer")]
    public class GetNutUpsFromServer : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public string ServerName { get; set; }

        [Parameter]
        public string UpsName { get; set; }

        protected override void ProcessRecord()
        {
            var callbackClient = new CallbackClient();
            var context = new InstanceContext(callbackClient);

            var factory = new DuplexChannelFactory<IManagementService>(
                context,
                new NetNamedPipeBinding(),
                new EndpointAddress("net.pipe://localhost/Wingnut"));

            var service = factory.CreateChannel();

            List<Ups> upsList =
                service.GetUpsFromServer(this.ServerName, this.UpsName).Result;

            this.WriteObject(upsList, true);
        }
    }


    [Cmdlet(VerbsCommon.Add, "NutUps")]
    public class AddNutUps : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public string ServerName { get; set; }

        [Parameter]
        public string UpsName { get; set; }

        [Parameter]
        public SwitchParameter MonitorOnly { get; set; }

        protected override void ProcessRecord()
        {
            var callbackClient = new CallbackClient();
            var context = new InstanceContext(callbackClient);

            var factory = new DuplexChannelFactory<IManagementService>(
                context,
                new NetNamedPipeBinding(),
                new EndpointAddress("net.pipe://localhost/Wingnut"));

            var service = factory.CreateChannel();

            Ups ups = service.AddUps(this.ServerName, this.UpsName, this.MonitorOnly).Result;

            this.WriteObject(ups);
        }
    }


    public class CallbackClient : IManagementCallback
    {
        public void SendCallbackMessage(string message)
        {
            throw new System.NotImplementedException();
        }
    }
}
