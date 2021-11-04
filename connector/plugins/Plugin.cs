using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Text;

namespace connector.plugins {
    public class Plugin
    {
        readonly String PluginPath = "./plugins/";
        readonly String ComponentPath = Environment.GetEnvironmentVariable("COMPONENTS_PATH") ?? "/app/components";

        public Plugin()
        {
            
        }

        public virtual string Name { get; set;}

        public virtual string Filename { 
            get
            {
                return this.Name + ".yaml";
            } 
            set
            {
               Filename = value; 
            }
        }

        public virtual List<string> EnvironmentVariables {get; set;}

        public virtual async Task ExecuteAsync(){
            Console.WriteLine($"Loaded {this.Name}");
            var linePattern = @".*?\${(\w+)}.*?";
            var evaluator = new MatchEvaluator(LineEvaluator);
            var stringBuilder = new StringBuilder();

            try
            {
                using (var reader = new StreamReader(PluginPath + Filename))
                {
                    string currentLine;
                    while ((currentLine = await reader.ReadLineAsync()) != null)
                    {
                        var newLine = Regex.Replace(currentLine,linePattern, evaluator);
                        stringBuilder.AppendLine(newLine);
                    }
                }

                await File.WriteAllTextAsync(ComponentPath + Filename, stringBuilder.ToString());
            }
            catch (IOException exception)
            {
                Console.WriteLine($"The YAML file {Filename} could not be parsed");
                Console.WriteLine(exception.Message);
            }
        }

        public static string LineEvaluator(Match lineMatch)
        {
            var keyPattern = @"(?<=\$\{)(.*)(?=\})";
            var replacePattern = @"!ENV \$\{.*\}$";
            var keyMatch = Regex.Match(lineMatch.Value, keyPattern, RegexOptions.ExplicitCapture);
            if(!keyMatch.Success)
            {
                return lineMatch.Value;
            }
            
            var key = keyMatch.Value;
            var value = Environment.GetEnvironmentVariable(key);
            if(null == value)
            {
                return lineMatch.Value;
            }

            return Regex.Replace(lineMatch.Value, replacePattern, value);
        }
    }
}
