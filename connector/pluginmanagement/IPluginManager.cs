using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using connector.plugins;

namespace connector
{
    public interface IPluginManager
    {
        public Task LoadAsync();
        public List<Plugin> Plugins { get; }
        public Guid Id {get;}
    }
}