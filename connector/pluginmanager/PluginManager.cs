using connector.plugins;
using connector.supervisor;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace connector
{
    public class PluginManager : IPluginManager
    {
        private readonly IServiceProvider _services;
        private readonly ILogger _logger;
        private readonly ISupervisorHandler _SupervisorHandler;
        public Guid Id { get; private set; }
        public List<Plugin> Plugins { get; private set; }

        public PluginManager(IServiceProvider services, ILogger logger, ISupervisorHandler supervisorHandler)
        {
            _logger = logger;
            _services = services;
            _SupervisorHandler = supervisorHandler;
            Plugins = new List<Plugin>();
            Id = Guid.NewGuid();
        }

        public async Task LoadAsync()
        {
            await _SupervisorHandler.GetTargetStateAsync();

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

            _logger.Information($"Loaded {loadedPlugins.Count} plugins");
            Plugins = loadedPlugins;
        }

        private async Task<Plugin> TryLoadingPluginAsync(Type pluginInstance)
        {
            try
            {
                var instance = (Plugin)ActivatorUtilities.CreateInstance(_services, pluginInstance);
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