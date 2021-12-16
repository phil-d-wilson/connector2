using connector.plugins;
using connector.supervisor;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace connector
{
    public class Connector
    {
        private List<Plugin> _plugins;
        private readonly ILogger _logger;
        private WebApplication _connectorServer;
        private LoggingLevelSwitch _levelSwitch;

        public Connector()
        {
            _levelSwitch = new LoggingLevelSwitch { MinimumLevel = LogEventLevel.Information};
            if((Environment.GetEnvironmentVariable("DEBUG") ?? "false") == "true")
            {
                _levelSwitch.MinimumLevel = LogEventLevel.Debug;
            }
            
            RegisterServices();
            _logger = _connectorServer.Services.GetRequiredService<ILogger>();
        }

        public async Task RunAsync()
        {
            var pluginManager = _connectorServer.Services.GetRequiredService<IPluginManager>();
            var daprManager = _connectorServer.Services.GetRequiredService<IDaprManager>();
            await pluginManager.LoadAsync();
            _plugins = pluginManager.Plugins;

            if (!daprManager.StartDaprProcess())
            {
                _logger.Error("Could not start daprd");

                return;
            }

            var daprComponents = await daprManager.GetLoadedComponentsAsync();

            ValidateSourcesAndSinks(daprComponents);
            RunGrpcService();
        }

        private void ValidateSourcesAndSinks(List<DaprComponent> daprComponents)
        {
            ValidateSource(daprComponents, "input");
            ValidateSource(daprComponents, "output");
        }

        private void ValidateSource(List<DaprComponent> daprComponents, string direction)
        {
            _logger.Information($"The following are {direction} components:");
            foreach (var plugin in _plugins.Where(p => p.Direction == direction))
            {
                if (!daprComponents.Where(c => c.Name == plugin.Name).Any())
                {
                    _logger.Warning($"{plugin.Name} was configured but dapr did not successfully load it.");
                    continue;
                }

                _logger.Information($"{plugin.Name}");
            }
        }

        private void RegisterServices()
        {
            Log.Logger = new LoggerConfiguration()
               .MinimumLevel.ControlledBy(_levelSwitch)
               .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Error)
               .WriteTo.Console(
                    outputTemplate: "[{Level}] {Message}{NewLine}{Exception}"
                )
               .CreateLogger();


            var builder = WebApplication.CreateBuilder(new string[] { });
            builder.Logging.AddSerilog();
            builder.WebHost.ConfigureKestrel((context, options) =>
            {
                options.ListenAnyIP(50051, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http2;
                });
            });
            builder.Services.AddGrpc();
            builder.Services.AddSingleton<IPluginManager, PluginManager>();
            builder.Services.AddSingleton<ISupervisorHandler, SupervisorHandler>();
            builder.Services.AddSingleton<IDaprManager, DaprManager>();
            builder.Services.AddSingleton<IEnvironmentHandler, EnvironmentHandler>();
            builder.Services.AddSingleton<IYamlResolver, YamlResolver>();
            builder.Services.AddSingleton(Log.Logger);
            builder.Services.AddSingleton<IPluginDependencyAggregate, PluginDependencyAggregate>();
            var serviceProvider = builder.Services.BuildServiceProvider(true);

            _connectorServer = builder.Build();
            _connectorServer.MapGrpcService<ConnectorServer>();
        }

        private void RunGrpcService()
        {
           _connectorServer.Run();
        }
    }

  
}