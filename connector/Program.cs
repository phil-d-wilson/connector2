using System.Threading.Tasks;
using System;
using Microsoft.Extensions.DependencyInjection;
using connector.supervisor;

namespace connector
{
    class Program
    {
        private static IServiceProvider _serviceProvider;
        public static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        private static async Task MainAsync(string[] args)
        {
            RegisterServices();
            IServiceScope scope = _serviceProvider.CreateScope();
            await scope.ServiceProvider.GetRequiredService<Connector>().RunAsync();
            DisposeServices();
        }

        private static void RegisterServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IPluginManager, PluginManager>();
            services.AddSingleton<ISupervisorHandler, SupervisorHandler>();
            services.AddSingleton<IDaprHandler, DaprHandler>();
            services.AddSingleton<Connector>();
            _serviceProvider = services.BuildServiceProvider(true);
        }

        private static void DisposeServices()
        {
            if (_serviceProvider == null)
            {
                return;
            }
            if (_serviceProvider is IDisposable)
            {
                ((IDisposable)_serviceProvider).Dispose();
            }
        }
    }
}
