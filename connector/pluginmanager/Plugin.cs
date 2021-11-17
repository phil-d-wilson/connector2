using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text.RegularExpressions;
using connector.supervisor;

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
                return this.Name + ".yaml";
            }
            set
            {
                Filename = value;
            }
        }

        public virtual IDictionary<string, string> ConfigurationEnvironmentVariables { get; set; }

        public ISupervisorHandler SupervisorHandler { private get; set; }

        public virtual async Task<bool> TryLoadAsync()
        {
            this.LoadDefaultConfigurationIfNeeded();
            if (this.AllEnvironmentVariablesSet())
            {
                Console.WriteLine($"{this.Name} plugin loaded.");
                if (await YamlResolver.ParseAndResolveYamlComponentFileAsync(this.Name, this.Filename))
                {
                    Console.WriteLine($"{this.Name} configured.");
                    return true;
                }
            }

            Console.WriteLine($"{this.Name} not configured.");
            return false;
        }

        private void LoadDefaultConfigurationIfNeeded()
        {
            var evaluator = new MatchEvaluator(Evaluator);

            foreach (var enVar in ConfigurationEnvironmentVariables)
            {
                if (null == Environment.GetEnvironmentVariable(enVar.Key) && null != enVar.Value)
                {
                    var value = Regex.Replace(enVar.Value, @"(\$\{[a-zA-Z\-]+\})", evaluator, RegexOptions.ExplicitCapture);

                    var method = enVar.Value == value ? "default" : "discovered";
                    Console.WriteLine($"{Name} setting {enVar.Key} to {method} value: {value}");
                    Environment.SetEnvironmentVariable(enVar.Key, value);
                }
            }
        }

        private Boolean AllEnvironmentVariablesSet()
        {
            var environmentVariables = Environment.GetEnvironmentVariables();
            var difference = this.ConfigurationEnvironmentVariables.Keys.Except((IEnumerable<string>)environmentVariables.Keys.Cast<string>().ToList());

            WarnIfOnlySomeEnvironmentVariablesAreSet(difference);

            return (0 == difference.Count());
        }

        private void WarnIfOnlySomeEnvironmentVariablesAreSet(IEnumerable<string> difference)
        {
            if ((difference.Count() > 0) && (difference.Count() < this.ConfigurationEnvironmentVariables.Keys.Count()))
            {
                Console.WriteLine($"Some environment variables configuring {this.Name} detected, but not all. The following are missing:");
                foreach (var enVar in difference)
                {
                    Console.WriteLine(enVar);
                }
            }
        }

        private string Evaluator(Match match)
        {
            if (SupervisorHandler.ServiceExistsInState(this.ServiceName))
            {
                var serviceDefinition = SupervisorHandler.GetServiceDefinition(this.ServiceName);

                if (("${service-address}" == match.Value) && (null != serviceDefinition.address))
                {
                    return serviceDefinition.address;
                }

                if (("${service-port}" == match.Value) && (null != serviceDefinition.port))
                {
                    return serviceDefinition.port;
                }
            }

            return match.Value;

        }
    }
}
