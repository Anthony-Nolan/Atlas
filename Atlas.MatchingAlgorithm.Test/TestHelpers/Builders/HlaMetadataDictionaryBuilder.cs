using System;
using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.Services;
using Atlas.HlaMetadataDictionary.Services.DataGeneration;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
using Atlas.HlaMetadataDictionary.WmdaDataAccess;
using NSubstitute;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers.Builders
{
    internal class HlaMetadataDictionaryBuilder : IHlaMetadataDictionaryFactory
    {
        private IRecreateHlaMetadataService recreate;
        private IAlleleNamesMetadataService name;
        private IHlaMatchingMetadataService matching;
        private ILocusHlaMatchingMetadataService locus;
        private IHlaScoringMetadataService scoring;
        private IHlaMetadataService all;
        private IDpb1TceGroupMetadataService dpb1;
        private IWmdaHlaNomenclatureVersionAccessor wmdaHlaNomenclatureVersionAccessor;
        private ILogger logger;
        private IHlaMetadataDictionary cannedResponse = null;

        public HlaMetadataDictionaryBuilder()
        {
            ResetAllDependencies();
        }

        private void ResetAllDependencies()
        {
            recreate = Substitute.For<IRecreateHlaMetadataService>();
            name = Substitute.For<IAlleleNamesMetadataService>();
            matching = Substitute.For<IHlaMatchingMetadataService>();
            locus = Substitute.For<ILocusHlaMatchingMetadataService>();
            scoring = Substitute.For<IHlaScoringMetadataService>();
            all = Substitute.For<IHlaMetadataService>();
            dpb1 = Substitute.For<IDpb1TceGroupMetadataService>();
            wmdaHlaNomenclatureVersionAccessor = Substitute.For<IWmdaHlaNomenclatureVersionAccessor>();
            logger = Substitute.For<ILogger>();
        }

        public HlaMetadataDictionaryBuilder Returning(IHlaMetadataDictionary cannedResponse)
        {
            this.cannedResponse = cannedResponse;
            return this;
        }

        public HlaMetadataDictionaryBuilder Using<T>(T dependency)
        {
            switch (dependency)
            {
                case IRecreateHlaMetadataService typedDependency:
                    recreate = typedDependency;
                    break;
                case IAlleleNamesMetadataService typedDependency:
                    name = typedDependency;
                    break;
                case IHlaMatchingMetadataService typedDependency:
                    matching = typedDependency;
                    break;
                case ILocusHlaMatchingMetadataService typedDependency:
                    locus = typedDependency;
                    break;
                case IHlaScoringMetadataService typedDependency:
                    scoring = typedDependency;
                    break;
                case IHlaMetadataService typedDependency:
                    all = typedDependency;
                    break;
                case IDpb1TceGroupMetadataService typedDependency:
                    dpb1 = typedDependency;
                    break;
                case IWmdaHlaNomenclatureVersionAccessor typedDependency:
                    wmdaHlaNomenclatureVersionAccessor = typedDependency;
                    break;
                default:
                    throw new InvalidOperationException($"Type '{typeof(T).FullName}' does not match any expected dependency");
            }

            return this;
        }

        public IHlaMetadataDictionary BuildDictionary(string activeVersion)
        {
            if (cannedResponse != null)
            {
                return cannedResponse;
            }

            return new Atlas.HlaMetadataDictionary.ExternalInterface.HlaMetadataDictionary(
                activeVersion,
                recreate,
                name,
                matching,
                locus,
                scoring,
                all,
                dpb1,
                wmdaHlaNomenclatureVersionAccessor,
                logger
            );
        }

        public IHlaMetadataCacheControl BuildCacheControl(string version)
        {
            throw new NotImplementedException();
        }
    }
}
