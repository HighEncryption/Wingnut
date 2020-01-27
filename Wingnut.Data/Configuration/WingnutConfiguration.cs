namespace Wingnut.Data.Configuration
{
    using System.Collections.Generic;

    public class WingnutConfiguration
    {
        public const string FileName = "configuration.json";

        public List<ServerConfiguration> Servers { get; set; }

        public WingnutConfiguration()
        {
            this.Servers = new List<ServerConfiguration>();
        }
    }
}