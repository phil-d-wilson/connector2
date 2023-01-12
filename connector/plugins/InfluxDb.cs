
using System.Collections.Generic;

namespace connector.plugins
{
    public class InfluxDb : Plugin
    {
        public InfluxDb(IPluginDependencyAggregate dependencyAggregate) : base(dependencyAggregate)
        {
            Name = "InfluxDb";
            Direction = "output";
            ServiceName = "InfluxDb";
            ConfigurationEnvironmentVariables = new Dictionary<string, string> {
                {"INFLUX_URL", "http://localhost:8086"},
                {"INFLUX_TOKEN", "balena:balena"},
                {"INFLUX_BUCKET", "balena/autogen"},
                {"INFLUX_ORG", "balena"}
            };
        }
    }
}