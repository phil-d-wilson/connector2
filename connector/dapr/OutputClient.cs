using connector.plugins;
using Dapr.AppCallback.Autogen.Grpc.v1;
using Dapr.Client.Autogen.Grpc.v1;
using Google.Protobuf.Collections;
using Grpc.Net.Client;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Dapr.Client.Autogen.Grpc.v1.Dapr;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace connector.dapr
{
    public class OutputClient : IOutputClient
    {
        private readonly DaprClient _client;
        private readonly ILogger _logger;
        private readonly IPluginManager _pluginManager;
        private List<Plugin> _outputBindings;

        public OutputClient(ILogger logger, IPluginManager pluginManager)
        {
            _logger = logger;
            _logger.Debug("Constructing gRPC output client");
            _pluginManager = pluginManager;
            var daprGrpcPort = Environment.GetEnvironmentVariable("DAPR_GRPC_PORT") ?? "50001";
            var channel = GrpcChannel.ForAddress("http://localhost:" + daprGrpcPort);
            _client = new DaprClient(channel);
        }

        public async Task SendMessageAsync(BindingEventRequest request)
        {
            if (null == _outputBindings)
            {
                _outputBindings = new List<Plugin>();
                foreach (var plugin in _pluginManager.Plugins.Where(p => p.Direction == "output"))
                {
                    _logger.Information($"Subscribing gRPC server to output component: {plugin.Name}");
                    _outputBindings.Add(plugin);
                }
            }

            foreach (var outputBinding in _outputBindings)
            {
                try
                {
                    var meta = new MapField<string, string>();

                    foreach (var metadata in outputBinding.GetMetadata(request.Data.ToStringUtf8()))
                    {
                        meta.Add(metadata.Key, metadata.Value);
                    }

                    var outputBindingEventRequest = new InvokeBindingRequest
                    {
                        Data = request.Data,
                        Operation = outputBinding.OutputOperation,
                        Metadata = { meta }
                    };

                    outputBindingEventRequest.Name = outputBinding.Name;

                    _logger.Information($"Sending data to {outputBinding} binding");
                    var result = await _client.InvokeBindingAsync(outputBindingEventRequest);
                }
                catch (Exception ex)
                {
                    _logger.Warning($"Failed to send message to {outputBinding.Name}. Exception details in debug log.");
                    _logger.Debug($"Exception: {ex.Message}");
                    var httpClient = new HttpClient();
                    await httpClient.PostAsJsonAsync("http://localhost:3500/V1.0/bindings/" + outputBinding.Name, request.Data);
                }
            }
        }
    }
}
