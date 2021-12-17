using Dapr.AppCallback.Autogen.Grpc.v1;
using Dapr.Client.Autogen.Grpc.v1;
using Grpc.Net.Client;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Dapr.Client.Autogen.Grpc.v1.Dapr;

namespace connector.dapr
{
    public class OutputClient : IOutputClient
    {
        private readonly DaprClient _client;
        private readonly ILogger _logger;
        private readonly IPluginManager _pluginManager;
        private List<string> _outputBindings;

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
                _outputBindings = new List<string>();
                foreach (var plugin in _pluginManager.Plugins.Where(p => p.Direction == "output"))
                {
                    _logger.Information($"Subscribing gRPC server to component: {plugin.Name}");
                    _outputBindings.Add(plugin.Name);
                }
            }

            var outputBindingEventRequest = new InvokeBindingRequest
            {
                Data = request.Data,
                Operation = "create"
            };

            _logger.Warning($"Request metadata: {request.Metadata}");

            foreach (var outputBinding in _outputBindings)
            {
                try
                {
                    outputBindingEventRequest.Name = outputBinding;
                    _logger.Information($"Sending data to {outputBinding} binding");
                    var result = await _client.InvokeBindingAsync(outputBindingEventRequest);
                }
                catch (Exception ex)
                {
                    _logger.Warning($"Failed to send message to {outputBinding}. Exception details in debug log.");
                    _logger.Debug($"Exception: {ex.Message}");
                }
            }
        }
    }
}
