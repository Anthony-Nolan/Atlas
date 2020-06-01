using System;
using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.Services;
using Atlas.HlaMetadataDictionary.Services.DataGeneration;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
using NSubstitute;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers.Builders
{
    internal class HlaMetadataDictionaryBuilder : IHlaMetadataDictionaryFactory
    {
        private IRecreateHlaMetadataService recreate;
        private IAlleleNamesLookupService name;
        private IHlaMatchingLookupService matching;
        private ILocusHlaMatchingLookupService locus;
        private IHlaScoringLookupService scoring;
        private IHlaLookupResultsService all;
        private IDpb1TceGroupLookupService dpb1;
        private IWmdaHlaVersionAccessor wmdaVersion;
        private ILogger logger;
        private IHlaMetadataDictionary cannedResponse = null;

        public HlaMetadataDictionaryBuilder()
        {
            ResetAllDependencies();
        }

        private void ResetAllDependencies()
        {
            recreate = Substitute.For<IRecreateHlaMetadataService>();
            name = Substitute.For<IAlleleNamesLookupService>();
            matching = Substitute.For<IHlaMatchingLookupService>();
            locus = Substitute.For<ILocusHlaMatchingLookupService>();
            scoring = Substitute.For<IHlaScoringLookupService>();
            all = Substitute.For<IHlaLookupResultsService>();
            dpb1 = Substitute.For<IDpb1TceGroupLookupService>();
            wmdaVersion = Substitute.For<IWmdaHlaVersionAccessor>();
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
                case IAlleleNamesLookupService typedDependency:
                    name = typedDependency;
                    break;
                case IHlaMatchingLookupService typedDependency:
                    matching = typedDependency;
                    break;
                case ILocusHlaMatchingLookupService typedDependency:
                    locus = typedDependency;
                    break;
                case IHlaScoringLookupService typedDependency:
                    scoring = typedDependency;
                    break;
                case IHlaLookupResultsService typedDependency:
                    all = typedDependency;
                    break;
                case IDpb1TceGroupLookupService typedDependency:
                    dpb1 = typedDependency;
                    break;
                case IWmdaHlaVersionAccessor typedDependency:
                    wmdaVersion = typedDependency;
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
                wmdaVersion,
                logger
            );
        }

        public IHlaMetadataCacheControl BuildCacheControl(string version)
        {
            throw new NotImplementedException();
        }
    }
}
