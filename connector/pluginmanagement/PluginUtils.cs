using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using JsonCons.JsonPath;
using Serilog;
using System.Linq;

namespace connector
{
    public static class PluginUtils
    {
        public static string GetUnixTimestamp()
        {
            return ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds().ToString();
        }

        //TODO document this!
        public static string GetValueFromJson(ILogger logger, string jsonPath, string json)
        {
            if (null == jsonPath)
            {
                return null;
            }

            try
            {
                var value = JsonSelector.Parse(jsonPath).Select(JsonDocument.Parse(json).RootElement);
                return value.FirstOrDefault().ToString();
            }
            catch (Exception ex)
            {
                logger.Error($"Value could not be parsed from input message using the path: {jsonPath}");
                logger.Debug($"Input message being parsed: {json}");
                logger.Debug($"Exception message: {ex.Message}");
                return null;
            }
        }
    }
}