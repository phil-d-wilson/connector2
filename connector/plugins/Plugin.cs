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
        private readonly ILogger _logger;
        private readonly IYamlResolver _yamlResolver;
        private readonly ISupervisorHandler _SupervisorHandler;

        public Plugin(IPluginDependencyAggregate dependencyAggregate)
        {
            _logger = dependencyAggregate.Logger;
            _environmentHandler = dependencyAggregate.EnvironmentHandler;
            _yamlResolver = dependencyAggregate.YamlResolver;
            _SupervisorHandler = dependencyAggregate.SupervisorHandler;
        }

        public async virtual Task<bool> TryLoadAsync()
        {
            LoadDefaultConfigurationIfNeeded();
            if (AllEnvironmentVariablesSet())
            {
                _logger.Information($"{Name} plugin loaded.");
                if (await _yamlResolver.ParseAndResolveYamlComponentFileAsync(Name, Filename))
                {
                    _logger.Information($"{Name} configured.");
                    return true;
                }
            }

            _logger.Warning($"{Name} not configured.");
            return false;
        }

        private void LoadDefaultConfigurationIfNeeded()
        {
            var evaluator = new MatchEvaluator(Evaluator);

            foreach (var enVar in ConfigurationEnvironmentVariables)
            {
                if (null == _environmentHandler.GetEnvironmentVariable(enVar.Key) && null != enVar.Value)
                {
                    var value = Regex.Replace(enVar.Value, @"(\$\{[a-zA-Z\-]+\})", evaluator, RegexOptions.ExplicitCapture);

                    var method = enVar.Value == value ? "default" : "discovered";
                    _logger.Information($"{Name} setting {enVar.Key} to {method} value: {value}");
                    _environmentHandler.SetEnvironmentVariable(enVar.Key, value);
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
                _logger.Warning($"Some environment variables configuring {Name} detected, but not all. The following are missing:");
                foreach (var enVar in difference)
                {
                    _logger.Warning(enVar);
                }
            }
        }

        private string Evaluator(Match match)
        {
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

            return match.Value;
        }
    }

    public class PluginDependencyAggregate : IPluginDependencyAggregate
    {
        public IEnvironmentHandler EnvironmentHandler { get; }
        public IYamlResolver YamlResolver { get; }
        public ILogger Logger { get; }
        public ISupervisorHandler SupervisorHandler { get; }

        public PluginDependencyAggregate(IEnvironmentHandler environmentHandler, IYamlResolver yamlResolver, ISupervisorHandler supervisorHandler, ILogger logger )
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
