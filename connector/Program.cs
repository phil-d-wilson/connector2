using System.Threading.Tasks;

namespace connector
{
    class Program
    {
        
        public static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        private static async Task MainAsync(string[] args)
        {
            var connector = new Connector();
            await connector.RunAsync();
        }

       
    }
}
