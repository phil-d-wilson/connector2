using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Linq;
using Serilog;

namespace connector.supervisor
{
    public class SupervisorHandler : ISupervisorHandler
    {
        private readonly List<ServiceDefinition> _serviceDefinitions;
        private readonly ILogger _logger;

        public SupervisorHandler(ILogger logger)
        {
            _serviceDefinitions = new List<ServiceDefinition>() { };
            _logger = logger;
        }
        public async Task GetTargetStateAsync()
        {
            _logger.Information("Getting target state from the supervisor");
            var httpClient = new HttpClient();
            var jsonDocumentOptions = new JsonDocumentOptions { AllowTrailingCommas = true };
            try
            {
                var supervisorAddress = Environment.GetEnvironmentVariable("BALENA_SUPERVISOR_ADDRESS");
                if (null == supervisorAddress)
                {
                    _logger.Error($"Please add the label `io.balena.features.supervisor-api` to your docker-compose file for the connector service.");
                    return;
                }

                var supervisorApiKey = Environment.GetEnvironmentVariable("BALENA_SUPERVISOR_API_KEY");
                if (null == supervisorAddress)
                {
                    _logger.Error($"Please add the label `io.balena.features.supervisor-api` to your docker-compose file for the connector service.");
                    return;
                }

                var response = await httpClient.GetAsync($"{supervisorAddress}/v2/local/target-state?apikey={supervisorApiKey}");
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();
                var document = JsonDocument.Parse(responseBody, jsonDocumentOptions);
                var appId = document.RootElement.GetProperty("state").GetProperty("local").GetProperty("apps").ToString().Split('\"')[1];
                foreach (var element in document.RootElement.GetProperty("state").GetProperty("local").GetProperty("apps").GetProperty(appId).GetProperty("services").EnumerateArray())
                {
                    var serviceName = GetServiceNameOrDefault(element);
                    if (serviceName == "connector")
                    {
                        continue;
                    }
                    var networkMode = GetNetworkModeOrDefault(element);
                    // _logger.Info($"Expose: {element.GetProperty("config").GetProperty("expose")}");
                    var port = GetPortOrDefault(element);
                    var networks = GetNetworksOrDefault(element);

                    string address = null;
                    if (null != networks)
                    {
                        address = GetAddress(networkMode, (JsonElement)networks);
                    }

                    _serviceDefinitions.Add(new ServiceDefinition
                    {
                        Name = serviceName,
                        Port = port,
                        Address = address
                    });
                }
            }
            catch (Exception e)
            {
                _logger.Error("\nException Caught!");
                _logger.Error("Message :{0} ", e.Message);
            }
        }

        private static string GetAddress(string networkMode, JsonElement networks)
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

        private static string GetServiceNameOrDefault(JsonElement element)
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

        private static JsonElement? GetNetworksOrDefault(JsonElement element)
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

        private static string GetNetworkModeOrDefault(JsonElement element)
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

        private static string GetPortOrDefault(JsonElement element)
        {
            try
            {
                var ports = element.GetProperty("config").GetProperty("portMaps").EnumerateArray();
                if (!ports.Any())
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
            return (_serviceDefinitions.Any(s => s.Name == serviceName));
        }

        public ServiceDefinition GetServiceDefinition(string serviceName)
        {
            return _serviceDefinitions.Where(s => s.Name == serviceName).FirstOrDefault();
        }
    }

    public class ServiceDefinition
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string Port { get; set; }
    }
}