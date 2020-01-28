namespace Wingnut.PowerShell
{
    using System.Management.Automation;
    using System.ServiceModel;

    using Wingnut.Channels;
    using Wingnut.Data;
    using Wingnut.Data.Models;

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
}