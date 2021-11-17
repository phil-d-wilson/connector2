using System.Collections.Generic;
using System.Threading.Tasks;
using connector.plugins;

namespace connector
{
    public interface IPluginManager
    {
        public Task<List<Plugin>> LoadAsync();
    }
}