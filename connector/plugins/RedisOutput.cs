
using System.Collections.Generic;
using System;

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
                {"REDIS_HOST", "${service-address}:${service-port}"},
                {"REDIS_USERNAME", " "},
                {"REDIS_PASSWORD", " "}
            };
        }

        public override List<KeyValuePair<string, string>> GetMetadata(string input)
        {
            var key = PluginUtils.GetValueFromJson(base.Logger, Environment.GetEnvironmentVariable("REDIS_KEY_PATH"), input);
            if (null == key)
            {
                base.Logger.Information("Using default (timestamp) for the redis key");
                key = PluginUtils.GetUnixTimestamp();
            }

            var output = new List<KeyValuePair<string, string>>
            {
                    new KeyValuePair<string, string>("key", key)
            };

            return output;
        }
    }
}