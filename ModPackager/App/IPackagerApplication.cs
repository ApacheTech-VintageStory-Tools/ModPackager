using System.Threading.Tasks;

namespace ModPackager.App
{
    public interface IPackagerApplication
    {
        Task RunAsync(params string[] args);
    }
}