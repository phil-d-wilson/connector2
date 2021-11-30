using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Text;
using Serilog;

namespace connector
{
    public class YamlResolver : IYamlResolver
    {
        private readonly ILogger _logger;

        public YamlResolver(ILogger logger)
        {
            _logger = logger;
        }


        public async Task<bool> ParseAndResolveYamlComponentFileAsync(string name, string filename)
        {
            var PluginPath = "./plugins/";
            var ComponentPath = Environment.GetEnvironmentVariable("COMPONENTS_PATH") ?? "/app/components";

            var linePattern = @".*?\${(\w+)}.*?";
            var evaluator = new MatchEvaluator(YamlFileLineEvaluator);
            var stringBuilder = new StringBuilder();

            try
            {
                using (var reader = new StreamReader(PluginPath + filename))
                {
                    string currentLine;
                    while ((currentLine = await reader.ReadLineAsync()) != null)
                    {
                        var newLine = Regex.Replace(currentLine, linePattern, evaluator);
                        stringBuilder.AppendLine(newLine);
                    }
                }

                await File.WriteAllTextAsync(ComponentPath + "/" + filename, stringBuilder.ToString());
                return true;
            }
            catch (Exception exception)
            {
                _logger.Error($"The file {filename} could not be parsed:");
                _logger.Error(exception.Message);
                return false;
            }
        }

        private string YamlFileLineEvaluator(Match lineMatch)
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
            _logger.Debug($"Replacing {key} with {value}");
            if (null == value)
            {
                return lineMatch.Value;
            }

            return Regex.Replace(lineMatch.Value, replacePattern, value);
        }
    }
}