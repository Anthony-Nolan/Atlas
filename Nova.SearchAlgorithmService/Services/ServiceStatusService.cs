using System.Reflection;
using Nova.SearchAlgorithmService.Models;

namespace Nova.SearchAlgorithmService.Services
{
    public interface IServiceStatusService
    {
        ServiceStatus GetServiceStatus();
    }

    public class ServiceStatusService : IServiceStatusService
    {
        public ServiceStatus GetServiceStatus()
        {
            Assembly assembly = GetType().Assembly;
            string name = assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
            string version = assembly.GetName().Version.ToString();

            return new ServiceStatus
            {
                Name = name,
                Version = version
            };
        }

    }
}