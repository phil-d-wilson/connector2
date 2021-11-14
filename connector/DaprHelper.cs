using System;
using System.Threading.Tasks;
using Dapr.Client;
using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;

namespace connector
{
    public class DaprHelper
    {
        private string daprPort;
        private DaprClient client;

        public DaprHelper()
        {
            daprPort = Environment.GetEnvironmentVariable("DAPR_PORT") ?? "3500";
            client = new DaprClientBuilder().Build();
        }
        public async Task<List<DaprComponent>> GetLoadedComponentsAsync()
        {
            Console.WriteLine("Dapr running. Finding loaded components");
            List<DaprComponent> components = new List<DaprComponent> { };
            var jsonDocumentOptions = new JsonDocumentOptions
            {
                AllowTrailingCommas = true
            };

            var httpClient = new HttpClient();
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync($"http://localhost:{daprPort}/v1.0/metadata");
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                var json = JsonDocument.Parse(responseBody, jsonDocumentOptions);
                components = JsonSerializer.Deserialize<List<DaprComponent>>(json.RootElement.GetProperty("components").ToString());

            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
            }

            if (components.Count == 0)
            {
                Console.WriteLine("Dapr either failed to start correctly, or loaded no components. Exiting");
                return null;
            }

            return components;
        }
    }

    public class DaprComponent
    {
        public string name { get; set; }
        public string type { get; set; }
        public string version { get; set; }
    }
}