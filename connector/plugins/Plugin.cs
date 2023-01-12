using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text.RegularExpressions;
using connector.supervisor;
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
        private readonly ISupervisorHandler _SupervisorHandler;

        public Plugin(IPluginDependencyAggregate dependencyAggregate)
        {
            Logger = dependencyAggregate.Logger;
            _environmentHandler = dependencyAggregate.EnvironmentHandler;
            _yamlResolver = dependencyAggregate.YamlResolver;
            _SupervisorHandler = dependencyAggregate.SupervisorHandler;

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
            var supervisorDiscovery = new MatchEvaluator(SupervisorDiscovery);

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
                        Logger.Debug($"{enVar.Key} has a default value of {enVar.Value}.");
                        //We need to see if the default, is actually a template value which we can try to auto-discover via the supervisor state
                        string value;
                        try
                        {
                            value = Regex.Replace(enVar.Value, @"(\$\{[a-zA-Z\-]+\})", supervisorDiscovery, RegexOptions.ExplicitCapture);
                        }
                        catch(KeyNotFoundException ex)
                        {
                            Logger.Debug($"{enVar.Key} matched auto-discovery pattern, but no service matched. Value not set.");
                            continue;
                        }

                        var method = enVar.Value == value ? "default" : "discovered";
                        Logger.Information($"{Name} setting {enVar.Key} to {method} value: {value}");
                        _environmentHandler.SetEnvironmentVariable(enVar.Key, value);
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

        private string SupervisorDiscovery(Match match)
        {
            if(null == ServiceName)
            {
                //nothing to match
                // return null;
                throw new KeyNotFoundException();
            }

            Logger.Debug($"{match.Value} matches the auto-discovery pattern. Checking for service {ServiceName}");
            if (_SupervisorHandler.ServiceExistsInState(ServiceName))
            {
                var serviceDefinition = _SupervisorHandler.GetServiceDefinition(ServiceName);

                if (("${service-address}" == match.Value) && (null != serviceDefinition.Address))
                {
                    return serviceDefinition.Address;
                }

                if (("${service-port}" == match.Value) && (null != serviceDefinition.Port))
                {
                    return serviceDefinition.Port;
                }
            }

            throw new KeyNotFoundException();
        }
    }

    public class PluginDependencyAggregate : IPluginDependencyAggregate
    {
        public IEnvironmentHandler EnvironmentHandler { get; }
        public IYamlResolver YamlResolver { get; }
        public ILogger Logger { get; }
        public ISupervisorHandler SupervisorHandler { get; }

        public PluginDependencyAggregate(IEnvironmentHandler environmentHandler, IYamlResolver yamlResolver, ISupervisorHandler supervisorHandler, ILogger logger)
        {
            EnvironmentHandler = environmentHandler;
            YamlResolver = yamlResolver;
            SupervisorHandler = supervisorHandler;
            Logger = logger;
        }
    }

    public interface IPluginDependencyAggregate
    {
        public IEnvironmentHandler EnvironmentHandler { get; }
        public IYamlResolver YamlResolver { get; }
        public ILogger Logger { get; }
        public ISupervisorHandler SupervisorHandler { get; }
    }
}
