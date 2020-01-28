namespace Wingnut.PowerShell
{
    using System.Management.Automation;

    using Wingnut.Data.Models;

    [Cmdlet(VerbsCommon.Add, "NutUps")]
    public class AddNutUps : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public string ServerName { get; set; }

        [Parameter]
        public string UpsName { get; set; }

        [Parameter]
        public SwitchParameter MonitorOnly { get; set; }

        [Parameter]
        public SwitchParameter Force { get; set; }

        protected override void ProcessRecord()
        {
            ServiceHelper helper = ServiceHelper.Create();

            Ups ups = helper.Channel
                .AddUps(this.ServerName, this.UpsName, this.MonitorOnly, this.Force.ToBool())
                .Result;

            this.WriteObject(ups);
        }
    }
}