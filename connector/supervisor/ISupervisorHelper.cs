using System.Threading.Tasks;

namespace connector.supervisor
{
    public interface ISupervisorHandler
    {
        public Task GetTargetStateAsync();

        public bool ServiceExistsInState(string serviceName);
        public ServiceDefinition GetServiceDefinition(string serviceName);
    }
}