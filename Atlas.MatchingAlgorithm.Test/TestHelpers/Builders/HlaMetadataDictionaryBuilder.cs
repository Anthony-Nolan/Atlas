using Atlas.HlaMetadataDictionary.Services;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.MatchingDictionary;
using NSubstitute;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers.Builders
{
    internal class HlaMetadataDictionaryBuilder
    {
        IRecreateHlaMetadataService recreate;
        IAlleleNamesLookupService name;
        IHlaMatchingLookupService matching;
        ILocusHlaMatchingLookupService locus;
        IHlaScoringLookupService scoring;
        IHlaLookupResultsService all;
        IDpb1TceGroupLookupService dpb1;
        IActiveHlaVersionAccessor activeVersion;
        IWmdaHlaVersionProvider wmdaVersion;

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
            activeVersion = Substitute.For<IActiveHlaVersionAccessor>();
            wmdaVersion = Substitute.For<IWmdaHlaVersionProvider>();
        }

        public HlaMetadataDictionaryBuilder Using<T>(T dependency)
        {
            switch (dependency)
            {
                case IRecreateHlaMetadataService typedDependency: recreate = typedDependency; break;
                case IAlleleNamesLookupService typedDependency: name = typedDependency; break;
                case IHlaMatchingLookupService typedDependency: matching = typedDependency; break;
                case ILocusHlaMatchingLookupService typedDependency: locus = typedDependency; break;
                case IHlaScoringLookupService typedDependency: scoring = typedDependency; break;
                case IHlaLookupResultsService typedDependency: all = typedDependency; break;
                case IDpb1TceGroupLookupService typedDependency: dpb1 = typedDependency; break;
                case IActiveHlaVersionAccessor typedDependency: activeVersion = typedDependency; break;
                case IWmdaHlaVersionProvider typedDependency: wmdaVersion = typedDependency; break;
            }

            return this;
        }

        public IHlaMetadataDictionary Build()
        {
            return new MatchingAlgorithm.Services.MatchingDictionary.HlaMetadataDictionary(
                recreate,
                name,
                matching,
                locus,
                scoring,
                all,
                dpb1,
                activeVersion,
                wmdaVersion
                    );
        }
    }
}
