using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;

namespace Atlas.MatchingAlgorithm.Services.Donors
{
    public interface IDonorHlaExpanderFactory
    {
        IDonorHlaExpander BuildForSpecifiedHlaNomenclatureVersion(string hlaNomenclatureVersion);
        IDonorHlaExpander BuildForActiveHlaNomenclatureVersion();
    }

    public class DonorHlaExpanderFactory : IDonorHlaExpanderFactory
    {
        private readonly IHlaMetadataDictionaryFactory dictionaryFactory;
        private readonly IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor;
        private readonly IMatchingAlgorithmImportLogger logger;

        public DonorHlaExpanderFactory(
            IHlaMetadataDictionaryFactory dictionaryFactory,
            IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor,
            IMatchingAlgorithmImportLogger logger)
        {
            this.dictionaryFactory = dictionaryFactory;
            this.hlaNomenclatureVersionAccessor = hlaNomenclatureVersionAccessor;
            this.logger = logger;
        }

        public IDonorHlaExpander BuildForActiveHlaNomenclatureVersion()
        {
            return BuildForSpecifiedHlaNomenclatureVersion(hlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersion());
        }

        public IDonorHlaExpander BuildForSpecifiedHlaNomenclatureVersion(string hlaNomenclatureVersion)
        {
            var specifiedVersionDictionary = dictionaryFactory.BuildDictionary(hlaNomenclatureVersion);
            return new DonorHlaExpander(specifiedVersionDictionary, logger);
        }
    }
}
