using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.ExternalInterface;
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
        private readonly IActiveHlaVersionAccessor versionAccessor;
        private readonly ILogger logger;

        public DonorHlaExpanderFactory(
            IHlaMetadataDictionaryFactory dictionaryFactory,
            IActiveHlaVersionAccessor versionAccessor,
            ILogger logger)
        {
            this.dictionaryFactory = dictionaryFactory;
            this.versionAccessor = versionAccessor;
            this.logger = logger;
        }

        public IDonorHlaExpander BuildForActiveHlaNomenclatureVersion()
        {
            return BuildForSpecifiedHlaNomenclatureVersion(versionAccessor.GetActiveHlaNomenclatureVersion());
        }

        public IDonorHlaExpander BuildForSpecifiedHlaNomenclatureVersion(string hlaNomenclatureVersion)
        {
            var specifiedVersionDictionary = dictionaryFactory.BuildDictionary(hlaNomenclatureVersion);
            return new DonorHlaExpander(specifiedVersionDictionary, logger);
        }
    }
}
