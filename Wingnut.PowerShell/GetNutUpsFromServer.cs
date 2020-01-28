namespace Wingnut.PowerShell
{
    using System.Collections.Generic;
    using System.Management.Automation;

    using Wingnut.Data.Models;

    [Cmdlet(VerbsCommon.Get, "NutUpsFromServer")]
    public class GetNutUpsFromServer : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public string ServerName { get; set; }

        [Parameter]
        public string UpsName { get; set; }

        protected override void ProcessRecord()
        {
            ServiceHelper helper = ServiceHelper.Create();

            List<Ups> upsList =
                helper.Channel.GetUpsFromServer(this.ServerName, this.UpsName).Result;

            this.WriteObject(upsList, true);
        }
    }
}