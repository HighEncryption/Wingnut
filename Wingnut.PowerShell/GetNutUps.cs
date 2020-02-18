namespace Wingnut.PowerShell
{
    using System.Collections.Generic;
    using System.Management.Automation;

    using Wingnut.Data.Models;

    [Cmdlet(VerbsCommon.Get, "NutUps")]
    public class GetNutUps: PSCmdlet
    {
        [Parameter]
        public string ServerName { get; set; }

        [Parameter]
        public string UpsName { get; set; }

        protected override void ProcessRecord()
        {
            ServiceHelper helper = ServiceHelper.Create();

            List<Ups> ups = helper.Channel.GetUps(this.ServerName, this.UpsName);

            this.WriteObject(ups, true);
        }
    }
}