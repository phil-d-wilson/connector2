using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace connector.plugins
{
    public class MqttInput : Plugin
    {
        public MqttInput()
        {
            Name = "MqttInput";
            Direction = "input";
            ConfigurationEnvironmentVariables = new Dictionary<string, string> {
                {"MQTT_INPUT_SERVER", "tcp://localhost:1883"},
                {"MQTT_INPUT_TOPIC", "balena"},
                {"MQTT_INPUT_QOS", "1"}
            };
        }

        // private string GetServer()
        // {

        // }
    }
}