using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;
using Nova.SearchAlgorithm.Test.Validation.TestData.Resources.SpecificTestCases;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Builders
{
    public class HlaTypingCategorySetBuilder
    {
        private readonly PhenotypeInfo<HlaTypingResolution> resolutions;

        public HlaTypingCategorySetBuilder()
        {
            resolutions = new PhenotypeInfo<HlaTypingResolution>(HlaTypingResolution.Tgs);
        }

        public HlaTypingCategorySetBuilder WithAllLociAtTypingResolution(HlaTypingResolution resolution)
        {
            foreach (var locus in LocusHelpers.AllLoci())
            {
                resolutions.SetAtLocus(locus, resolution);
            }
            return this;
        }

        public HlaTypingCategorySetBuilder WithTypingResolutionAtLocus(Locus locus, HlaTypingResolution resolution)
        {
            resolutions.SetAtLocus(locus, resolution);
            return this;
        }

        public HlaTypingCategorySetBuilder UntypedAtLocus(Locus locus)
        {
            return WithTypingResolutionAtLocus(locus, HlaTypingResolution.Untyped);
        }

        public HlaTypingCategorySetBuilder WithDifferentlyTypedLoci()
        {
            foreach (var resolution in TestCaseTypingResolutions.DifferentLociResolutions)
            {
                resolutions.SetAtLocus(resolution.Key, resolution.Value);
            }

            return this;
        }

        public PhenotypeInfo<HlaTypingResolution> Build()
        {
            return resolutions;
        }
    }
}