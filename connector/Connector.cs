using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using connector.plugins;
using System.Linq;

namespace connector
{
    public class connector
    {
        private List<Plugin> plugins;
        private List<DaprComponent> daprComponents;

        public async Task RunAsync()
        {
            var pluginManager = new PluginManager();
            plugins = await pluginManager.LoadAsync();

            if (!StartDaprProcess())
            {
                Console.WriteLine("Could not start daprd");
                return;
            }
            var dapr = new DaprHelper();
            daprComponents = await dapr.GetLoadedComponentsAsync();

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
            foreach (var plugin in plugins.Where(p => p.Direction == direction))
            {
                if (daprComponents.Where(c => c.name == plugin.Name).Count() == 0)
                {
                    Console.WriteLine($"WARNING: {plugin.Name} was configured but dapr did not successfully load it.");
                    continue;
                }

                Console.WriteLine($"{plugin.Name}");
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