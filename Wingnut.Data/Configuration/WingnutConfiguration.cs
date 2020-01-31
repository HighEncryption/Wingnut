namespace Wingnut.Data.Configuration
{
    using System.Collections.Generic;

    public class WingnutConfiguration
    {
        public const string FileName = "configuration.json";

        public WingnutServiceConfiguration ServiceConfiguration { get; set; }

        public ShutdownConfiguration ShutdownConfiguration { get; set; }

        public List<UpsConfiguration> UpsConfigurations { get; set; }

        public WingnutConfiguration()
        {
            this.UpsConfigurations = new List<UpsConfiguration>();
        }
    }
}