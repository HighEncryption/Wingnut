namespace Wingnut.PowerShell
{
    using System;
    using System.Linq;
    using System.Management.Automation;
    using System.Net.Sockets;

    using Wingnut.Data;
    using Wingnut.Data.Models;

    [Cmdlet(VerbsCommon.Add, "NutUps")]
    public class AddNutUps : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public string Address { get; set; }

        [Parameter]
        public int Port { get; set; } = Constants.DefaultPortNumber;

        [Parameter(Mandatory = true)]
        public PSCredential Credential { get; set; }

        [Parameter]
        public SSLUsage UseSSL { get; set; } = SSLUsage.Optional;

        [Parameter]
        public string ServerSSLName { get; set; }

        [Parameter]
        public AddressFamily? PreferredAddressFamily { get; set; }

        [Parameter]
        public string UpsName { get; set; }

        [Parameter]
        public int NumPowerSupplies { get; set; } = Constants.DefaultNumPowerSupplies;

        [Parameter]
        public SwitchParameter MonitorOnly { get; set; }

        [Parameter]
        public SwitchParameter Force { get; set; }

        protected override void ProcessRecord()
        {
            ServiceHelper helper = ServiceHelper.Create();

            Server server = new Server()
            {
                Address = this.Address,
                Port = this.Port,
                Username = this.Credential.UserName,
                UseSSL = this.UseSSL,
                PreferredAddressFamily = this.PreferredAddressFamily,
                ServerSSLName = this.ServerSSLName,
            };

            try
            {
                Ups ups = helper.Channel.AddUps(
                        server,
                        this.Credential.Password.GetDecrypted(),
                        this.UpsName,
                        this.NumPowerSupplies,
                        this.MonitorOnly,
                        this.Force.ToBool())
                    .Result;

                this.WriteObject(ups);
            }
            catch (AggregateException e) when (e.InnerExceptions.Count == 1)
            {
                throw e.InnerExceptions.First();
            }
        }
    }
}