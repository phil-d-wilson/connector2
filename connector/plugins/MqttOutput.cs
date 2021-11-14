using System.Collections.Generic;

namespace connector.plugins
{
    public class MqttOutput : Plugin
    {
        public MqttOutput()
        {
            Name = "MqttOutput";
            Direction = "output";
            ConfigurationEnvironmentVariables = new Dictionary<string, string> {
                {"MQTT_OUTPUT_SERVER", null},
                {"MQTT_OUTPUT_TOPIC", null},
                {"MQTT_OUTPUT_QOS", null}
                } ;
        }

    }
}