using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace connector
{
    public class DaprManager : IDaprManager
    {
        private readonly string _daprPort;
        private readonly string _appComponents;
        private readonly string _daprGrpcPort;
        private readonly ILogger _logger;
        private readonly bool _debug;

        public DaprManager(ILogger logger)
        {
            _daprPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500";
            _daprGrpcPort = Environment.GetEnvironmentVariable("DAPR_GRPC_PORT") ?? "50001";
            _debug = (Environment.GetEnvironmentVariable("DEBUG") ?? "false") == "true";
            _appComponents = Environment.GetEnvironmentVariable("COMPONENTS_PATH") ?? "/app/components";
            _logger = logger;
        }

        public bool StartDaprProcess()
        {
            _logger.Information("Starting dapr");
            var dapr = new ProcessStartInfo("dapr")
            {
                Arguments = $"run --components-path {_appComponents} --app-protocol grpc --app-port 50051 --dapr-http-port {_daprPort} --dapr-grpc-port {_daprGrpcPort} --app-id connector"
            };

            dapr.RedirectStandardOutput = !_debug;

            var proc = Process.Start(dapr);
            //TODO: this sleep does not work - the whole thread blocks, including dapr finishing initialisation
            Thread.Sleep(5000); // need to wait for dapr to load and configure itself

            //Now start the dapr dashboard
            dapr = new ProcessStartInfo("dapr")
            {
                Arguments = $"dashboard"
            };
            Process.Start(dapr);

            return (null != proc);
        }
        public async Task<List<DaprComponent>> GetLoadedComponentsAsync()
        {
            _logger.Information("Dapr running. Finding loaded components");
            var components = new List<DaprComponent> { };
            var jsonDocumentOptions = new JsonDocumentOptions
            {
                AllowTrailingCommas = true
            };

            var httpClient = new HttpClient();
            try
            {
                var response = await httpClient.GetAsync($"http://localhost:{_daprPort}/v1.0/metadata");
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();
                var json = JsonDocument.Parse(responseBody, jsonDocumentOptions);
                components = JsonSerializer.Deserialize<List<DaprComponent>>(json.RootElement.GetProperty("components").ToString());

            }
            catch (HttpRequestException e)
            {
                _logger.Error("\nException Caught!");
                _logger.Error("Message :{0} ", e.Message);
            }

            if (components.Count == 0)
            {
                _logger.Error("Dapr either failed to start correctly, or loaded no components. Exiting");
                return null;
            }

            return components;
        }
    }

    public class DaprComponent
    {
        //these are lowercase, so that the json deserialisation works. :)
        public string name { get; set; }
        public string type { get; set; }
        public string version { get; set; }
    }
}