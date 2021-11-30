using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text.RegularExpressions;
using connector.supervisor;
using Microsoft.Extensions.DependencyInjection;
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
        private IEnvironmentHandler EnvironmentHandler { get; set; }
        private IServiceProvider _services;
        private ILogger _logger;

        public void Initialise(IServiceProvider services)
        {
            if (null == _services)
            {
                _services = services;
                EnvironmentHandler = services.GetService<IEnvironmentHandler>();
                _logger = services.GetService<ILogger>();
            }
        }

        public async virtual Task<bool> TryLoadAsync()
        {
            LoadDefaultConfigurationIfNeeded();
            if (AllEnvironmentVariablesSet())
            {
                _logger.Information($"{Name} plugin loaded.");
                if (await _services.GetRequiredService<IYamlResolver>().ParseAndResolveYamlComponentFileAsync(Name, Filename))
                {
                    _logger.Information($"{Name} configured.");
                    return true;
                }
            }

            _logger.Information($"{Name} not configured.");
            return false;
        }

        private void LoadDefaultConfigurationIfNeeded()
        {
            var evaluator = new MatchEvaluator(Evaluator);

            foreach (var enVar in ConfigurationEnvironmentVariables)
            {
                if (null == EnvironmentHandler.GetEnvironmentVariable(enVar.Key) && null != enVar.Value)
                {
                    var value = Regex.Replace(enVar.Value, @"(\$\{[a-zA-Z\-]+\})", evaluator, RegexOptions.ExplicitCapture);

                    var method = enVar.Value == value ? "default" : "discovered";
                    _logger.Information($"{Name} setting {enVar.Key} to {method} value: {value}");
                    EnvironmentHandler.SetEnvironmentVariable(enVar.Key, value);
                }
            }
        }

        private bool AllEnvironmentVariablesSet()
        {
            var environmentVariables = EnvironmentHandler.GetEnvironmentVariables();
            var difference = ConfigurationEnvironmentVariables.Keys.Except((IEnumerable<string>)environmentVariables.Keys.Cast<string>().ToList());

            WarnIfOnlySomeEnvironmentVariablesAreSet(difference);

            return (!difference.Any());
        }

        private void WarnIfOnlySomeEnvironmentVariablesAreSet(IEnumerable<string> difference)
        {
            if ((difference.Any()) && (difference.Count() < ConfigurationEnvironmentVariables.Keys.Count))
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
            var supervisorHandler = _services.GetRequiredService<ISupervisorHandler>();
            if (supervisorHandler.ServiceExistsInState(ServiceName))
            {
                var serviceDefinition = supervisorHandler.GetServiceDefinition(ServiceName);

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
}
