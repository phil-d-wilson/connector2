using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using connector.plugins;
using System.Linq;

namespace connector
{
    public class Connector
    {
        private List<Plugin> _plugins;
        private List<DaprComponent> _daprComponents;
        private IPluginManager _pluginManager;
        private IDaprHandler _daprHandler;

        public Connector(IPluginManager pluginManager, IDaprHandler daprHelper)
        {
            _pluginManager = pluginManager;
            _daprHandler = daprHelper;
        }

        public async Task RunAsync()
        {
            _plugins = await _pluginManager.LoadAsync();

            if (!StartDaprProcess())
            {
                Console.WriteLine("Could not start daprd");
                return;
            }
            _daprComponents = await _daprHandler.GetLoadedComponentsAsync();

            ValidateSourcesAndSinks();
        }

        private void ValidateSourcesAndSinks()
        {
            ValidateSource("input");
            ValidateSource("output");
        }

        private void ValidateSource(string direction)
        {
            Console.WriteLine($"The following are {direction} components:");
            foreach (var plugin in _plugins.Where(p => p.Direction == direction))
            {
                if (_daprComponents.Where(c => c.name == plugin.Name).Count() == 0)
                {
                    Console.WriteLine($"WARNING: {plugin.Name} was configured but dapr did not successfully load it.");
                    continue;
                }

                Console.WriteLine($"{plugin.Name}");
                //DO SOME WIRING HERE
            }
        }

        private static bool StartDaprProcess()
        {
            Console.WriteLine("Starting dapr");
            var dapr = new Process();
            dapr.StartInfo.FileName = @"./daprd";
            dapr.StartInfo.Arguments = @"--components-path /app/components --app-protocol grpc --app-port 50051 --app-id connector";
            bool daprDebug;
            if (Boolean.TryParse(Environment.GetEnvironmentVariable("HIDE_DAPR_OUTPUT") ?? "true", out daprDebug))
            {
                dapr.StartInfo.RedirectStandardOutput = daprDebug;
            }

            var proc = dapr.Start();
            Thread.Sleep(10); // need to wait for dapr to load and configure itself
            return proc;
        }
    }
}