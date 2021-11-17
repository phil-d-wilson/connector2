using System;
using System.Linq;
using System.Reflection;
using connector.plugins;
using System.Threading.Tasks;
using System.Collections.Generic;
using connector.supervisor;

namespace connector
{
    public class PluginManager : IPluginManager
    {
        private ISupervisorHandler _supervisorHandler;

        public PluginManager(ISupervisorHandler supervisorHandler)
        {
            _supervisorHandler = supervisorHandler;
        }

        public async Task<List<Plugin>> LoadAsync()
        {
            await _supervisorHandler.GetTargetStateAsync();

            Console.WriteLine("Finding plugins");
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
                    Console.WriteLine($"Adding {plugin.Name} as a {plugin.Direction} element.");
                }
            }

            Console.WriteLine("Done loading plugins");
            return loadedPlugins;
        }

        private async Task<Plugin> TryLoadingPluginAsync(Type pluginInstance)
        {
            try
            {
                var instance = (Plugin)Activator.CreateInstance(pluginInstance);
                instance.SupervisorHandler = _supervisorHandler;
                if (await instance.TryLoadAsync())
                {
                    return instance;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load {pluginInstance.Name} with exception: {ex.Message}");
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