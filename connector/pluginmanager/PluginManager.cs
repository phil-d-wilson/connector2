using System;
using System.Linq;
using System.Reflection;
using connector.plugins;
using System.Threading.Tasks;
using System.Collections.Generic;
using connector.supervisor;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace connector
{
    public class PluginManager : IPluginManager
    {
        private IServiceProvider _services;
        private ILogger _logger;

        public async Task<List<Plugin>> LoadAsync(IServiceProvider services)
        {
            if(null == _services)
            {
                _services = services;
                _logger = services.GetRequiredService<ILogger>();
            }

            await _services.GetRequiredService<ISupervisorHandler>().GetTargetStateAsync();

            _logger.Information("Finding plugins");
            var loadedPlugins = new List<Plugin> { };

            var types = GetApplicationTypes();
            foreach (var type in types)
            {
                //skip the base class
                if (type.ToString().StartsWith("connector.plugins.Plugin"))
                {
                    continue;
                }

                var plugin = await TryLoadingPluginAsync(type);

                if (null != plugin)
                {
                    loadedPlugins.Add(plugin);
                    _logger.Information($"Adding {plugin.Name} as a {plugin.Direction} element.");
                }
            }

            _logger.Information("Done loading plugins");
            return loadedPlugins;
        }

        private async Task<Plugin> TryLoadingPluginAsync(Type pluginInstance)
        {
            try
            {
                var instance = (Plugin)Activator.CreateInstance(pluginInstance);
                instance.Initialise(_services);
                if (await instance.TryLoadAsync())
                {
                    return instance;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to load {pluginInstance.Name} with exception: {ex.Message}");
            }

            return null;
        }
        private static IEnumerable<Type> GetApplicationTypes()
        {
            var types = (from t in Assembly.GetExecutingAssembly().GetTypes()
                         where t.IsClass && t.Namespace == "connector.plugins"
                         select t);

            return types;
        }
    }
}