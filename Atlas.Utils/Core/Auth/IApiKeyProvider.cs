using System.Threading.Tasks;

namespace Nova.Utils.Auth
{
    public interface IApiKeyProvider
    {
        Task<bool> IsValid(string apiKey);
    }
}
