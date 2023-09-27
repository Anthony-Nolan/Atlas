using Atlas.Common.FeatureManagement;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.FeatureManagement;
using System.Collections.Generic;

namespace Atlas.MatchingAlgorithm.Services.FeatureManagement
{
    public class MatchingAlgorithmFeatureManager : AtlasFeatureManager
    {
        public MatchingAlgorithmFeatureManager(IFeatureManagerSnapshot featureManagerSnapshot, IConfigurationRefresherProvider refresherProvider)
            : base(featureManagerSnapshot, refresherProvider)
        {
        }

        protected override List<string> SupportedFeatures => new()
        {
            FeatureFlags.UseDonorInfoStoredInMatchingAlgorithmDb
        };
    }
}
