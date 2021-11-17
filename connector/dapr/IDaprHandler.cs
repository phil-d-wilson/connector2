using System.Collections.Generic;
using System.Threading.Tasks;

namespace connector
{
    public interface IDaprHandler
    {
        public Task<List<DaprComponent>> GetLoadedComponentsAsync();
    }
}