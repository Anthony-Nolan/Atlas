using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.Services.DataGeneration;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
using Atlas.HlaMetadataDictionary.WmdaDataAccess;
using NSubstitute;
using System;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers.Builders
{
    internal class HlaMetadataDictionaryBuilder : IHlaMetadataDictionaryFactory
    {
        private IRecreateHlaMetadataService recreate;
        private IHlaMatchingMetadataService matching;
        private ILocusHlaMatchingMetadataService locus;
        private IHlaScoringMetadataService scoring;
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
            matching = Substitute.For<IHlaMatchingMetadataService>();
            locus = Substitute.For<ILocusHlaMatchingMetadataService>();
            scoring = Substitute.For<IHlaScoringMetadataService>();
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
                case IHlaMatchingMetadataService typedDependency:
                    matching = typedDependency;
                    break;
                case ILocusHlaMatchingMetadataService typedDependency:
                    locus = typedDependency;
                    break;
                case IHlaScoringMetadataService typedDependency:
                    scoring = typedDependency;
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
                matching,
                locus,
                scoring,
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
