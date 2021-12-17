using Dapr.AppCallback.Autogen.Grpc.v1;
using System.Threading.Tasks;

namespace connector.dapr
{
    public interface IOutputClient
    {
        Task SendMessageAsync(BindingEventRequest request);
    }
}