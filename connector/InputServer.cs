using connector.dapr;
using Dapr.AppCallback.Autogen.Grpc.v1;
using Dapr.Client.Autogen.Grpc.v1;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Serilog;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace connector
{
    public class InputServer : AppCallback.AppCallbackBase
    {
        private readonly ILogger _logger;
        private readonly IPluginManager _pluginManager;
        private readonly IOutputClient _outputClient;

        public InputServer(ILogger logger, IPluginManager pluginManager, IOutputClient outputClient)
        {
            _logger = logger;
            _logger.Debug("Constructing gRPC AppCallback service");
            _pluginManager = pluginManager;
            _logger.Debug($"gRPC service has {_pluginManager.Plugins.Count} plugins");
            _outputClient = outputClient;
        }

        public override Task<ListTopicSubscriptionsResponse> ListTopicSubscriptions(Empty request, ServerCallContext context)
        {
            _logger.Error($"ListTopicSubscriptions called with request {request}");
            return Task.FromResult(new ListTopicSubscriptionsResponse());
        }

        public override Task<ListInputBindingsResponse> ListInputBindings(Empty request, ServerCallContext context)
        {
            byte[] bytes;
            var bindings = new RepeatedField<string>();

            foreach (var plugin in _pluginManager.Plugins.Where(p => p.Direction == "input"))
            {
                _logger.Information($"Subscribing gRPC server to component: {plugin.Name}");
                bindings.Add(plugin.Name);
            }
            
            var response = new ListInputBindingsResponse
            {
                Bindings = { bindings }
            };

            using (var stream = new MemoryStream())
            {
                response.WriteTo(stream);
                bytes = stream.ToArray();
            }

            var output = ListInputBindingsResponse.Parser.ParseFrom(bytes);

            return Task.FromResult(output);
        }

        public override Task<InvokeResponse> OnInvoke(InvokeRequest request, ServerCallContext context)
        {
            _logger.Error($"OnInvoke not implemented.");
            return Task.FromResult(new InvokeResponse());
        }

        public override Task<TopicEventResponse> OnTopicEvent(TopicEventRequest request, ServerCallContext context)
        {
            _logger.Error($"OnTopicEvent not implemented");
            return Task.FromResult(new TopicEventResponse());
        }

        public async override Task<BindingEventResponse> OnBindingEvent(BindingEventRequest request, ServerCallContext context)
        {
            _logger.Debug($"Event from {request.Name} component: {request.Data.ToStringUtf8()}");
            await _outputClient.SendMessageAsync(request);
            return new BindingEventResponse();
        }

    }
}
