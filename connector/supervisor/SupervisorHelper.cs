using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace connector.supervisor
{
    public class SupervisorHandler : ISupervisorHandler
    {
        private List<ServiceDefinition> _serviceDefinitions;

        public SupervisorHandler()
        {
            _serviceDefinitions = new List<ServiceDefinition>() { };
        }
        public async Task GetTargetStateAsync()
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
                    return;
                }

                var supervisorApiKey = Environment.GetEnvironmentVariable("BALENA_SUPERVISOR_API_KEY");
                if (null == supervisorAddress)
                {
                    Console.WriteLine($"Please add the label `io.balena.features.supervisor-api` to your docker-compose file for the connector service.");
                    return;
                }

                HttpResponseMessage response = await httpClient.GetAsync($"{supervisorAddress}/v2/local/target-state?apikey={supervisorApiKey}");
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                var document = JsonDocument.Parse(responseBody, jsonDocumentOptions);
                var appId = document.RootElement.GetProperty("state").GetProperty("local").GetProperty("apps").ToString().Split('\"')[1];
                foreach (JsonElement element in document.RootElement.GetProperty("state").GetProperty("local").GetProperty("apps").GetProperty(appId).GetProperty("services").EnumerateArray())
                {
                    var serviceName = GetServiceNameOrDefault(element);
                    if (serviceName == "connector")
                    {
                        continue;
                    }
                    var networkMode = GetNetworkModeOrDefault(element);
                    // Console.WriteLine($"Expose: {element.GetProperty("config").GetProperty("expose")}");
                    var port = GetPortOrDefault(element);
                    var networks = GetNetworksOrDefault(element);

                    string address = null;
                    if (null != networks)
                    {
                        address = GetAddress(networkMode, (JsonElement)networks);
                    }

                    _serviceDefinitions.Add(new ServiceDefinition
                    {
                        name = serviceName,
                        port = port,
                        address = address
                    });
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
            }
        }

        private string GetAddress(string networkMode, JsonElement networks)
        {
            if ("host" == networkMode)
            {
                return "localhost";
            }

            try
            {
                var aliases = networks.GetProperty("1_default").GetProperty("aliases").EnumerateArray();
                return aliases.FirstOrDefault().ToString();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private string GetServiceNameOrDefault(JsonElement element)
        {
            try
            {
                return element.GetProperty("serviceName").ToString();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private JsonElement? GetNetworksOrDefault(JsonElement element)
        {
            try
            {
                return element.GetProperty("config").GetProperty("networks");
            }
            catch (Exception)
            {
                return null;
            }
        }

        private string GetNetworkModeOrDefault(JsonElement element)
        {
            try
            {
                return element.GetProperty("config").GetProperty("networkMode").ToString();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private string GetPortOrDefault(JsonElement element)
        {
            try
            {
                var ports = element.GetProperty("config").GetProperty("portMaps").EnumerateArray();
                if (0 == ports.Count())
                {
                    return null;
                }

                return ports.First().GetProperty("ports").GetProperty("internalStart").ToString();

            }
            catch (Exception)
            {
                return null;
            }
        }

        public bool ServiceExistsInState(string serviceName)
        {
            return (_serviceDefinitions.Count(s => s.name == serviceName) > 0);
        }

        public ServiceDefinition GetServiceDefinition(string serviceName)
        {
            return _serviceDefinitions.Where(s => s.name == serviceName).FirstOrDefault();
        }
    }

    public class ServiceDefinition
    {
        public string name { get; set; }
        public string address { get; set; }
        public string port { get; set; }
    }
}