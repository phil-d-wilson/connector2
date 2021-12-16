using System.Collections.Generic;
using System.Threading.Tasks;

namespace connector
{
    public interface IDaprManager
    {
        public Task<List<DaprComponent>> GetLoadedComponentsAsync();
        public bool StartDaprProcess();
    }
}