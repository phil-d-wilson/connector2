using System.Collections.Generic;

namespace connector.plugins
{
    public class AzureEventHub : Plugin
    {
        public AzureEventHub()
        {
            Name = "AzureEventHub";
            EnvironmentVariables = new List<string> {
                    "AZURE_EH_CONNECTIONSTRING",
                    "AZURE_EH_CONSUMER_GROUP",
                    "AZURE_EH_STORAGE_ACCOUNT",
                    "AZURE_EH_STORAGE_ACCOUNT_KEY",
                    "AZURE_EH_CONTAINER_NAME"
                } ;
        }

    }
}