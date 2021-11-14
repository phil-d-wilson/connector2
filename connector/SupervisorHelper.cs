using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace connector
{
    public class SupervisorHelper
    {
        public async Task<string> GetTargetStateAsync()
        {
            Console.WriteLine("Getting target state from the supervisor");
            var httpClient = new HttpClient();
            var jsonDocumentOptions = new JsonDocumentOptions { AllowTrailingCommas = true };
            try
            {
                var supervisorAddress = Environment.GetEnvironmentVariable("BALENA_SUPERVISOR_ADDRESS");
                if (null == supervisorAddress)
                {
                    Console.WriteLine($"Please add the label `io.balena.features.supervisor-api` to your docker-compose file for the connector service.");
                    return null;
                }

                var supervisorApiKey = Environment.GetEnvironmentVariable("BALENA_SUPERVISOR_API_KEY");
                if (null == supervisorAddress)
                {
                    Console.WriteLine($"Please add the label `io.balena.features.supervisor-api` to your docker-compose file for the connector service.");
                    return null;
                }

                HttpResponseMessage response = await httpClient.GetAsync($"{supervisorAddress}/v2/local/target-state?apikey={supervisorApiKey}");
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                var document = JsonDocument.Parse(responseBody, jsonDocumentOptions);
                var appId = document.RootElement.GetProperty("state").GetProperty("local").GetProperty("apps").ToString().Split('\"')[1];
                foreach (JsonElement element in document.RootElement.GetProperty("state").GetProperty("local").GetProperty("apps").GetProperty(appId).GetProperty("services").EnumerateArray())
                {
                    Console.WriteLine($"Service name: {element.GetProperty("serviceName")}");
                    Console.WriteLine($"Network mode: {element.GetProperty("config").GetProperty("networkMode")}");
                    Console.WriteLine($"Expose: {element.GetProperty("config").GetProperty("expose")}");
                    Console.WriteLine($"Port mapping: {element.GetProperty("config").GetProperty("portMaps")}");
                    Console.WriteLine($"Networks: {element.GetProperty("config").GetProperty("networks")}");
                    Console.WriteLine("-------------------------------");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
            }

            return null;
        }
    }
}