namespace Wingnut.PowerShell
{
    using System.Management.Automation;

    [Cmdlet(VerbsCommon.Remove, "NutServer")]
    public class RemoveNutServer : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public string Name { get; set; }

        protected override void ProcessRecord()
        {
            ServiceHelper helper = ServiceHelper.Create();

            helper.Channel.DeleteServer(this.Name);
        }
    }
}