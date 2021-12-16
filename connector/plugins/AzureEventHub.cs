using System.Collections.Generic;
using System;

namespace connector.plugins
{
    public class AzureEventHub : Plugin
    {
        public AzureEventHub(IPluginDependencyAggregate dependencyAggregate) : base(dependencyAggregate)
        {
            Name = "AzureEventHub";
            Direction = Environment.GetEnvironmentVariable("AZURE_EH_DIRECTION") ?? "output";
            ConfigurationEnvironmentVariables = new Dictionary<string, string> {
                    {"AZURE_EH_CONNECTIONSTRING", null},
                    {"AZURE_EH_CONSUMER_GROUP", null},
                    {"AZURE_EH_STORAGE_ACCOUNT", null},
                    {"AZURE_EH_STORAGE_ACCOUNT_KEY", null},
                    {"AZURE_EH_CONTAINER_NAME", null}
                };
        }

    }
}