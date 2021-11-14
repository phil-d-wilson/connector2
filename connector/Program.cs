using System.Threading.Tasks;
using System;

namespace connector
{
    class Program
    {
        public static void Main()
        {
            MainAsync().GetAwaiter().GetResult();
        }

        private static async Task MainAsync()
        {
            Console.WriteLine("start");
            var connector = new connector();
            await connector.RunAsync();
        }
    }
}
