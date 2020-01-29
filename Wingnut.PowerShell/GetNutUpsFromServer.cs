namespace Wingnut.PowerShell
{
    using System.Collections.Generic;
    using System.Management.Automation;

    using Wingnut.Data;
    using Wingnut.Data.Models;

    [Cmdlet(VerbsCommon.Get, "NutUpsFromServer")]
    public class GetNutUpsFromServer : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public string Address { get; set; }

        [Parameter]
        public int Port { get; set; } = Constants.DefaultPortNumber;

        [Parameter(Mandatory = true)]
        public PSCredential Credential { get; set; }

        [Parameter]
        public string UpsName { get; set; }

        protected override void ProcessRecord()
        {
            ServiceHelper helper = ServiceHelper.Create();

            Server server = new Server()
            {
                Address = this.Address,
                Port = this.Port,
                Username = this.Credential.UserName,
            };

            List<Ups> upsList =
                helper.Channel.GetUpsFromServer(
                    server, 
                    this.Credential.Password.GetDecrypted(),
                    this.UpsName).Result;

            this.WriteObject(upsList, true);
        }
    }
}