using System.Threading.Tasks;

namespace connector
{
    public interface IYamlResolver
    {
        Task<bool> ParseAndResolveYamlComponentFileAsync(string name, string filename);
    }
}