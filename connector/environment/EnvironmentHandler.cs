using System;
using System.Collections;

namespace connector
{
    public class EnvironmentHandler : IEnvironmentHandler
    {
        public IDictionary GetEnvironmentVariables()
        {
            return Environment.GetEnvironmentVariables();
        }

        public string? GetEnvironmentVariable(string key)
        {
            return Environment.GetEnvironmentVariable(key);
        }

        public void SetEnvironmentVariable(string key, string? value)
        {
            Environment.SetEnvironmentVariable(key, value);
        }
    }
}
