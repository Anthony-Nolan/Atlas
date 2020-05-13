using System;
using Atlas.HlaMetadataDictionary.Repositories;
using Atlas.HlaMetadataDictionary.Services;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.MatchingDictionary;
using NSubstitute;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers.Builders
{
    internal class HlaMetadataDictionaryBuilder : IHlaMetadataDictionaryFactory
    {
        IRecreateHlaMetadataService recreate;
        IAlleleNamesLookupService name;
        IHlaMatchingLookupService matching;
        ILocusHlaMatchingLookupService locus;
        IHlaScoringLookupService scoring;
        IHlaLookupResultsService all;
        IDpb1TceGroupLookupService dpb1;
        IWmdaHlaVersionProvider wmdaVersion;
        IHlaMatchingLookupRepository repo;
        private IHlaMetadataDictionary cannedResponse = null;

        public HlaMetadataDictionaryBuilder()
        {
            ResetAllDependencies();
        }

        public void ResetAllDependencies()
        {
            recreate = Substitute.For<IRecreateHlaMetadataService>();
            name = Substitute.For<IAlleleNamesLookupService>();
            matching = Substitute.For<IHlaMatchingLookupService>();
            locus = Substitute.For<ILocusHlaMatchingLookupService>();
            scoring = Substitute.For<IHlaScoringLookupService>();
            all = Substitute.For<IHlaLookupResultsService>();
            dpb1 = Substitute.For<IDpb1TceGroupLookupService>();
            wmdaVersion = Substitute.For<IWmdaHlaVersionProvider>();
            repo = Substitute.For<IHlaMatchingLookupRepository>();
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
                case IWmdaHlaVersionProvider typedDependency:
                    wmdaVersion = typedDependency;
                    break;
                case IHlaMatchingLookupRepository typedDependency:
                    repo = typedDependency;
                    break;
                default:
                    throw new InvalidOperationException($"Type '{typeof(T).FullName}' does not match any expected dependency");
            }

            return this;
        }

        public IHlaMetadataDictionary BuildDictionary(string activeVersion)
        {
            return BuildDictionary(new HlaMetadataConfiguration { ActiveWmdaVersion = activeVersion});
        }

        public IHlaMetadataDictionary BuildDictionary(HlaMetadataConfiguration config)
        {
            if (cannedResponse != null)
            {
                return cannedResponse;
            }

            return new MatchingAlgorithm.Services.MatchingDictionary.HlaMetadataDictionary(
                config,
                recreate,
                name,
                matching,
                locus,
                scoring,
                all,
                dpb1,
                wmdaVersion,
                repo
            );
        }

        public IHlaMetadataCacheControl BuildCacheControl(string version)
        {
            throw new NotImplementedException();
        }

        public IHlaMetadataCacheControl BuildCacheControl(HlaMetadataConfiguration config)
        {
            throw new NotImplementedException();
        }
    }
}
