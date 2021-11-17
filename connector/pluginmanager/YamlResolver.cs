using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Text;

namespace connector
{
    public static class YamlResolver
    {
        public static async Task<bool> ParseAndResolveYamlComponentFileAsync(string name, string filename)
        {
            String PluginPath = "./plugins/";
            String ComponentPath = Environment.GetEnvironmentVariable("COMPONENTS_PATH") ?? "/app/components";

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
                Console.WriteLine($"The file {filename} could not be parsed:");
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