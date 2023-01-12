using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
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
        private readonly string _appId;

        public SupervisorHandler(ILogger logger)
        {
            _serviceDefinitions = new List<ServiceDefinition>() { };
            _logger = logger;
            _appId = Environment.GetEnvironmentVariable("BALENA_APP_ID");
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

                if ((JsonNode.Parse(responseBody)["state"]["local"]["apps"]).ToString() == "{}")
                {
                    _logger.Information("The balena Supervisor target state shows no release running. Continuing without auto-discovery functionality.");
                    return;
                }

                var services = (JsonArray)JsonNode.Parse(responseBody)["state"]["local"]["apps"][_appId]["services"];
                _logger.Debug($"Supervisor target state shows {services.Count} services in running release.");
                string connectorNetworkMode = GetConnectorNetworkModeOrDefault(services);
                foreach (var service in services)
                {
                    var serviceName = service["serviceName"].ToString();
                    if (serviceName == "connector")
                    {
                        continue;
                    }
                    _logger.Debug($"Assessing service: {serviceName}");
                    var networkMode = GetNetworkModeOrDefault(service);
                    var port = GetPortOrDefault(service);
                    if (null == port)
                    {
                        _logger.Warning($"Could not discover a port for {serviceName}. Service not added to auto-discovery.");
                        continue;
                    }
                    var networks = GetNetworksOrDefault(service);

                    string address = null;
                    if (null != networks)
                    {
                        address = GetAddress(_appId, networkMode, connectorNetworkMode, (JsonNode)networks);
                        if(null == address)
                        {
                            _logger.Warning($"Could not discover an address for {serviceName}. Service not added to auto-discovery.");
                            continue;
                        }
                    }

                    _serviceDefinitions.Add(new ServiceDefinition
                    {
                        Name = serviceName,
                        Port = port,
                        Address = address
                    });
                    _logger.Debug($"Service discovered: {serviceName} on port {port} at {address}");
                }
            }
            catch (Exception e)
            {
                _logger.Error("\nException Caught!");
                _logger.Error("Message :{0} ", e.Message);
                _logger.Information("Unable to find or connect to a balena supervisor - continuing without auto-discovery functionality.");
            }
        }

        private string GetConnectorNetworkModeOrDefault(JsonArray services)
        {
            string output = null;
            foreach (var service in services)
            {
                if (service["serviceName"].ToString() != "connector")
                {
                    continue;
                }

                output = GetNetworkModeOrDefault(service);
            }

            if (null == output)
            {
                _logger.Error("Could not find a service called Connector. Make sure the docker-compose names this block Connector. Setting network mode to 'bridged' but this may not be correct!");
                return "bridge";
            }

            return output;
        }

        private static string GetAddress(string appId, string networkMode, string connectorNetworkMode, JsonNode networks)
        {
            if ("host" == networkMode || "host" == connectorNetworkMode)
            {
                return "localhost";
            }

            try
            {
                var aliases = networks[appId + "_default"]["aliases"];
                return ""; //TODO WTF is this?
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string GetServiceNameOrDefault(JsonNode element)
        {
            try
            {
                return element["serviceName"].ToString();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static JsonNode GetNetworksOrDefault(JsonNode node)
        {
            try
            {
                return node["config"]["networks"];
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string GetNetworkModeOrDefault(JsonNode node)
        {
            try
            {
                return node["config"]["networkMode"].ToString();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string GetPortOrDefault(JsonNode node)
        {
            try
            {
                var ports = node["config"]["portMaps"];
                if (((JsonArray)ports).Count < 1)
                {
                    return null;
                }

                return ((JsonArray)ports).First()["ports"]["internalStart"].ToString();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public bool ServiceExistsInState(string serviceName)
        {
            return (_serviceDefinitions.Any(s => s.Name.ToLower() == serviceName.ToLower()));
        }

        public ServiceDefinition GetServiceDefinition(string serviceName)
        {
            return _serviceDefinitions.Where(s => s.Name.ToLower() == serviceName.ToLower()).FirstOrDefault();
        }
    }

    public class ServiceDefinition
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string Port { get; set; }
    }
}