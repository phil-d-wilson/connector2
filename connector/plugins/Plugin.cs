using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Serilog;

namespace connector.plugins
{
    public class Plugin
    {
        public virtual string Name { get; set; }

        public virtual string Direction { get; set; }

        public virtual string ServiceName { get; set; }

        public virtual string OutputOperation { get; set; }

        public virtual string Filename
        {
            get
            {
                return Name + ".yaml";
            }
            set
            {
                Filename = value;
            }
        }

        public virtual IDictionary<string, string> ConfigurationEnvironmentVariables { get; set; }
        private readonly IEnvironmentHandler _environmentHandler;
        internal readonly ILogger Logger;
        private readonly IYamlResolver _yamlResolver;

        public Plugin(IPluginDependencyAggregate dependencyAggregate)
        {
            Logger = dependencyAggregate.Logger;
            _environmentHandler = dependencyAggregate.EnvironmentHandler;
            _yamlResolver = dependencyAggregate.YamlResolver;

            OutputOperation = "create";
        }

        public virtual List<KeyValuePair<string, string>> GetMetadata(string input)
        {
            return new List<KeyValuePair<string, string>>();
        }

        public async virtual Task<bool> TryLoadAsync()
        {
            Logger.Debug($"Trying to load plugin: {Name}");
            LoadDefaultConfigurationIfNeeded();
            if (AllEnvironmentVariablesSet())
            {
                Logger.Debug($"{Name} plugin found.");
                if (await _yamlResolver.ParseAndResolveYamlComponentFileAsync(Name, Filename))
                {
                    Logger.Information($"{Name} configured and loaded.");
                    return true;
                }
            }

            Logger.Information($"{Name} not configured.");
            return false;
        }

        private void LoadDefaultConfigurationIfNeeded()
        {
            foreach (var enVar in ConfigurationEnvironmentVariables)
            {
                Logger.Debug($"Assessing plugin environment variable: {enVar.Key}");
                //If the environment variable has not been set at a fleet or device level
                if (null == _environmentHandler.GetEnvironmentVariable(enVar.Key))
                {
                    Logger.Debug($"{enVar.Key} has not been set as a device or fleet variable.");
                    //If there is a value set in the plugin itself
                    if (null != enVar.Value)
                    {
                        Logger.Information($"{enVar.Key} has a default value of {enVar.Value}.");
                        _environmentHandler.SetEnvironmentVariable(enVar.Key, enVar.Value);
                    }
                    else
                    {
                        Logger.Debug($"{enVar.Key} has no default value.");
                    }
                }
                else
                {
                    Logger.Information($"{enVar.Key} set as device or fleet variable: {enVar.Value}");
                }
            }
        }

        private bool AllEnvironmentVariablesSet()
        {
            var environmentVariables = _environmentHandler.GetEnvironmentVariables();
            var difference = ConfigurationEnvironmentVariables.Keys.Except(environmentVariables.Keys.Cast<string>().ToList());

            WarnIfOnlySomeEnvironmentVariablesAreSet(difference);

            return (!difference.Any());
        }

        private void WarnIfOnlySomeEnvironmentVariablesAreSet(IEnumerable<string> difference)
        {
            if (difference.Any() && (difference.Count() < ConfigurationEnvironmentVariables.Keys.Count))
            {
                Logger.Warning($"Some environment variables configuring {Name} detected, but not all. The following are missing:");
                foreach (var enVar in difference)
                {
                    Logger.Warning(enVar);
                }
            }
        }
    }

    public class PluginDependencyAggregate : IPluginDependencyAggregate
    {
        public IEnvironmentHandler EnvironmentHandler { get; }
        public IYamlResolver YamlResolver { get; }
        public ILogger Logger { get; }

        public PluginDependencyAggregate(IEnvironmentHandler environmentHandler, IYamlResolver yamlResolver, ILogger logger)
        {
            EnvironmentHandler = environmentHandler;
            YamlResolver = yamlResolver;
            Logger = logger;
        }
    }

    public interface IPluginDependencyAggregate
    {
        public IEnvironmentHandler EnvironmentHandler { get; }
        public IYamlResolver YamlResolver { get; }
        public ILogger Logger { get; }
    }
}
