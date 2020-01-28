namespace Wingnut.PowerShell
{
    using System.Collections.Generic;
    using System.Management.Automation;

    using Wingnut.Data.Models;

    [Cmdlet(VerbsCommon.Get, "NutServer")]
    public class GetNutServer : PSCmdlet
    {
        [Parameter]
        public string Name { get; set; }

        protected override void ProcessRecord()
        {
            ServiceHelper helper = ServiceHelper.Create();

            List<Server> servers = helper.Channel.GetServers(this.Name);

            this.WriteObject(servers, true);
        }
    }
}
