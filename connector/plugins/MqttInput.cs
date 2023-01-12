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
                {"MQTT_SERVER", "tcp://locahost:1883"},
                {"MQTT_TOPIC", "balena"},
                {"MQTT_QOS", "1"}
            };
        }
    }
}