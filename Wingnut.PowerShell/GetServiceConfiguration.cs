namespace Wingnut.PowerShell
{
    using System.Management.Automation;

    using Wingnut.Data.Configuration;

    [Cmdlet(VerbsCommon.Get, "ServiceConfiguration")]
    public class GetServiceConfiguration : PSCmdlet
    {
        protected override void ProcessRecord()
        {
            ServiceHelper helper = ServiceHelper.Create();
            WingnutServiceConfiguration configuration = helper.Channel.GetServiceConfiguration();
            this.WriteObject(configuration);
        }
    }
}