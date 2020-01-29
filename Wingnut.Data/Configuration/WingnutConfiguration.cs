namespace Wingnut.Data.Configuration
{
    using System.Collections.Generic;

    public class WingnutConfiguration
    {
        public const string FileName = "configuration.json";

        public List<ServerConfiguration> Servers { get; set; }

        public int MinimumPowerSupplies { get; set; }

        public WingnutConfiguration()
        {
            this.MinimumPowerSupplies = 1;
            this.Servers = new List<ServerConfiguration>();
        }
    }
}