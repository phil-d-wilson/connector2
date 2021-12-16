using System.Collections.Generic;

namespace connector.plugins
{
    public class MqttInput : Plugin
    {
        public MqttInput(IPluginDependencyAggregate dependencyAggregate) : base(dependencyAggregate)
        {
            Name = "MqttInput";
            Direction = "input";
            ServiceName = "mqtt";
            ConfigurationEnvironmentVariables = new Dictionary<string, string> {
                {"MQTT_INPUT_SERVER", "tcp://${service-address}:${service-port}"},
                {"MQTT_INPUT_TOPIC", "balena"},
                {"MQTT_INPUT_QOS", "1"}
            };
        }
    }
}