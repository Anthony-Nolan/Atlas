using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.FeatureManagement;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.Common.FeatureManagement
{
    public interface IAtlasFeatureManager
    {
        Task<bool> IsFeatureEnabled(string featureName);
    }

    public class AtlasFeatureManager : IAtlasFeatureManager
    {
        private readonly IFeatureManager featureManagerSnapshot;
        private readonly IConfigurationRefresher configurationRefresher;

        private readonly Dictionary<string, bool> features = new();

        public AtlasFeatureManager(IFeatureManagerSnapshot featureManagerSnapshot, IConfigurationRefresherProvider refresherProvider)
        {
            this.featureManagerSnapshot = featureManagerSnapshot;
            configurationRefresher = refresherProvider.Refreshers.First();
        }

        public async Task<bool> IsFeatureEnabled(string featureName)
        {
            if (features.TryGetValue(featureName, out var value))
                return value;

            await configurationRefresher.TryRefreshAsync();

            var valueFromAzure = await featureManagerSnapshot.IsEnabledAsync(featureName);
            features[featureName] = valueFromAzure;
            return valueFromAzure;
        }
    }
}
