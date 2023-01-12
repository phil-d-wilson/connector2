using System.Collections.Generic;
using System;

namespace connector.plugins
{
    public class MqttInput : Plugin
    {
        public MqttInput(IPluginDependencyAggregate dependencyAggregate) : base(dependencyAggregate)
        {
            Name = "MqttInput";
            Direction = Environment.GetEnvironmentVariable("MQTT_DIRECTION") ?? "input";
            ServiceName = "mqtt";
            ConfigurationEnvironmentVariables = new Dictionary<string, string> {
                {"MQTT_SERVER", "tcp://${service-address}:${service-port}"},
                {"MQTT_TOPIC", "balena"},
                {"MQTT_QOS", "1"}
            };
        }
    }
}