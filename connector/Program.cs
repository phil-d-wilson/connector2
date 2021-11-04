using System;
using System.Linq;
using System.Reflection;
using connector.plugins;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace connector
{
    class Program
    {
        public static void Main()
        {
            MainAsync().GetAwaiter().GetResult();
        }

        private static async Task MainAsync()
        {
           await FindAndLoadPlugins();
        }

        private static async Task FindAndLoadPlugins()
        {
            Console.WriteLine("Finding plugins");
            var environmentVariables = Environment.GetEnvironmentVariables();

            var types = GetApplicationTypes();
            foreach(var type in types)
            {
                //skip the base class
                if(type.ToString().StartsWith("connector.plugins.Plugin"))
                {
                    continue;
                }
                await CreateAndActivatePluginAsync(type, environmentVariables);
            }
        }

        private static async Task CreateAndActivatePluginAsync(Type pluginInstance, System.Collections.IDictionary environmentVariables)
        {
            var instance = (Plugin)Activator.CreateInstance(pluginInstance);
            if(AllPluginEnvironmentVariablesSet(environmentVariables, instance))
            {
                await (instance).ExecuteAsync();
            }
        }

        private static Boolean AllPluginEnvironmentVariablesSet(System.Collections.IDictionary environmentVariables, Plugin instance)
        {
            var difference = instance.EnvironmentVariables.Except((IEnumerable<string>)environmentVariables.Keys.Cast<string>().ToList());
            return (0 == difference.Count());
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
