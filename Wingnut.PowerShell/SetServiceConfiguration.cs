namespace Wingnut.PowerShell
{
    using System.Management.Automation;

    using Wingnut.Data.Configuration;

    [Cmdlet(VerbsCommon.Set, "ServiceConfiguration")]
    public class SetServiceConfiguration : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public WingnutServiceConfiguration ServiceConfiguration { get; set; }

        protected override void ProcessRecord()
        {
            ServiceHelper helper = ServiceHelper.Create();
            helper.Channel.UpdateServiceConfiguration(this.ServiceConfiguration);
        }
    }
}