using Atlas.HlaMetadataDictionary;
using Atlas.Utils.Core.ApplicationInsights;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;

namespace Atlas.MatchingAlgorithm.Services.Donors
{
    public interface IDonorHlaExpanderFactory
    {
        IDonorHlaExpander BuildForSpecifiedHlaNomenclatureVersion(string hlaDatabaseVersion);
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
            return BuildForSpecifiedHlaNomenclatureVersion(versionAccessor.GetActiveHlaDatabaseVersion());
        }

        public IDonorHlaExpander BuildForSpecifiedHlaNomenclatureVersion(string hlaDatabaseVersion)
        {
            var specifiedVersionDictionary = dictionaryFactory.BuildDictionary(hlaDatabaseVersion);
            return new DonorHlaExpander(specifiedVersionDictionary, logger);
        }
    }
}
