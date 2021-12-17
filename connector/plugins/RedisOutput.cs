using System.Collections.Generic;

namespace connector.plugins
{
    public class RedisOutput : Plugin
    {
        public RedisOutput(IPluginDependencyAggregate dependencyAggregate) : base(dependencyAggregate)
        {
            Name = "RedisOutput";
            Direction = "output";
            ServiceName = "redis";
            ConfigurationEnvironmentVariables = new Dictionary<string, string> {
                {"REDIS_OUTPUT_HOST", "${service-address}:${service-port}"},
                {"REDIS_OUTPUT_USERNAME", " "},
                {"REDIS_OUTPUT_PASSWORD", " "}
            };
        }
    }
}