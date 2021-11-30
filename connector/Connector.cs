using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using connector.plugins;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using connector.supervisor;
using Serilog;

namespace connector
{
    public class Connector
    {
        private List<Plugin> _plugins;
        private IServiceProvider _services;
        private readonly ILogger _logger;

        public Connector()
        {
            RegisterServices();
            _logger = _services.GetRequiredService<ILogger>();
        }

        ~Connector()
        {
            DisposeServices();
        }

        public async Task RunAsync()
        {
            var pluginManager = _services.GetRequiredService<IPluginManager>();
            _plugins = await pluginManager.LoadAsync(_services);

            if (!StartDaprProcess())
            {
                _logger.Information("Could not start daprd");

                return;
            }
            var daprComponents = await _services.GetRequiredService<IDaprHandler>().GetLoadedComponentsAsync();

            ValidateSourcesAndSinks(daprComponents);
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
                if (!daprComponents.Where(c => c.name == plugin.Name).Any())
                {
                    _logger.Warning($"{plugin.Name} was configured but dapr did not successfully load it.");
                    continue;
                }

                _logger.Information($"{plugin.Name}");
                //DO SOME WIRING HERE
            }
        }

        private bool StartDaprProcess()
        {
            _logger.Information("Starting dapr");
            var dapr = new Process();
            dapr.StartInfo.FileName = @"./daprd";
            dapr.StartInfo.Arguments = @"--components-path /app/components --app-protocol grpc --app-port 50051 --app-id connector";
            bool daprDebug;
            if (bool.TryParse(Environment.GetEnvironmentVariable("HIDE_DAPR_OUTPUT") ?? "true", out daprDebug))
            {
                dapr.StartInfo.RedirectStandardOutput = daprDebug;
            }

            var proc = dapr.Start();
            Thread.Sleep(10); // need to wait for dapr to load and configure itself
            return proc;
        }

        private void RegisterServices()
        {
            Log.Logger = new LoggerConfiguration()
               .MinimumLevel.Debug()
               .WriteTo.Console()
               .CreateLogger();

            var services = new ServiceCollection();
            services.AddSingleton<IPluginManager, PluginManager>();
            services.AddSingleton<ISupervisorHandler, SupervisorHandler>();
            services.AddSingleton<IDaprHandler, DaprHandler>();
            services.AddSingleton<IEnvironmentHandler, EnvironmentHandler>();
            services.AddSingleton(Log.Logger); 
            var serviceProvider = services.BuildServiceProvider(true);
            var scope = serviceProvider.CreateScope();
            _services = scope.ServiceProvider;
        }

        private void DisposeServices()
        {
            if (_services == null)
            {
                return;
            }
            if (_services is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}