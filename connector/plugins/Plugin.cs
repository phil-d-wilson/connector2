using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Text;
using System.Linq;

namespace connector.plugins
{
    public class Plugin
    {
        readonly String PluginPath = "./plugins/";
        readonly String ComponentPath = Environment.GetEnvironmentVariable("COMPONENTS_PATH") ?? "/app/components";

        public Plugin()
        {

        }

        public virtual string Name { get; set; }

        public virtual string Direction { get; set; }

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

        public virtual async Task<bool> TryLoadAsync()
        {
            this.LoadDefaultConfigurationIfNeeded();
            if (this.AllEnvironmentVariablesSet())
            {
                if (await this.ParseAndResolveYamlComponentFileAsync())
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
            foreach (var enVar in ConfigurationEnvironmentVariables)
            {
                if (null == Environment.GetEnvironmentVariable(enVar.Key) && null != enVar.Value)
                {
                    Console.WriteLine($"{Name} setting {enVar.Key} to default value: {enVar.Value}");
                    Environment.SetEnvironmentVariable(enVar.Key, enVar.Value);
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

        private async Task<bool> ParseAndResolveYamlComponentFileAsync()
        {
            Console.WriteLine($"{this.Name} plugin loaded.");
            var linePattern = @".*?\${(\w+)}.*?";
            var evaluator = new MatchEvaluator(YamlFileLineEvaluator);
            var stringBuilder = new StringBuilder();

            try
            {
                using (var reader = new StreamReader(PluginPath + Filename))
                {
                    string currentLine;
                    while ((currentLine = await reader.ReadLineAsync()) != null)
                    {
                        var newLine = Regex.Replace(currentLine, linePattern, evaluator);
                        stringBuilder.AppendLine(newLine);
                    }
                }

                await File.WriteAllTextAsync(ComponentPath + "/" + Filename, stringBuilder.ToString());
                return true;
            }
            catch (Exception exception)
            {
                Console.WriteLine($"The file {Filename} could not be parsed:");
                Console.WriteLine(exception.Message);
                return false;
            }
        }

        private static string YamlFileLineEvaluator(Match lineMatch)
        {
            var keyPattern = @"(?<=\$\{)(.*)(?=\})";
            var replacePattern = @"!ENV \$\{.*\}$";
            var keyMatch = Regex.Match(lineMatch.Value, keyPattern, RegexOptions.ExplicitCapture);
            if (!keyMatch.Success)
            {
                return lineMatch.Value;
            }

            var key = keyMatch.Value;
            var value = Environment.GetEnvironmentVariable(key);
            // Console.WriteLine($"Replacing {key} with {value}");
            if (null == value)
            {
                return lineMatch.Value;
            }

            return Regex.Replace(lineMatch.Value, replacePattern, value);
        }
    }
}
