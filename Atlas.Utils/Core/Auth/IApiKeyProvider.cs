using System.Threading.Tasks;

namespace Atlas.Utils.Core.Auth
{
    public interface IApiKeyProvider
    {
        Task<bool> IsValid(string apiKey);
    }
}
