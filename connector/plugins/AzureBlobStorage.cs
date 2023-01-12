using System.Collections.Generic;

namespace connector.plugins
{
    public class AzureBlobStorage : Plugin
    {
        public AzureBlobStorage(IPluginDependencyAggregate dependencyAggregate) : base(dependencyAggregate)
        {
            Name = "AzureBlobStorage";
            Direction = "output";
            ConfigurationEnvironmentVariables = new Dictionary<string, string> {
                    {"AZURE_BLOB_STORAGE_ACCOUNT_NAME", null},
                    {"AZURE_BLOB_CONTAINER_NAME", null}
                };
        }

    }
}