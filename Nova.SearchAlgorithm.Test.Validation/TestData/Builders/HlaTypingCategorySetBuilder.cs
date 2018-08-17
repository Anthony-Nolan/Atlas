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
            resolutions = new PhenotypeInfo<HlaTypingResolution>
            {
                A_1 = HlaTypingResolution.Arbitrary,
                A_2 = HlaTypingResolution.Arbitrary,
                B_1 = HlaTypingResolution.Arbitrary,
                B_2 = HlaTypingResolution.Arbitrary,
                C_1 = HlaTypingResolution.Arbitrary,
                C_2 = HlaTypingResolution.Arbitrary,
                DPB1_1 = HlaTypingResolution.Arbitrary,
                DPB1_2 = HlaTypingResolution.Arbitrary,
                DQB1_1 = HlaTypingResolution.Arbitrary,
                DQB1_2 = HlaTypingResolution.Arbitrary,
                DRB1_1 = HlaTypingResolution.Arbitrary,
                DRB1_2 = HlaTypingResolution.Arbitrary,
            };
        }

        public HlaTypingCategorySetBuilder WithAllLociAtTypingResolution(HlaTypingResolution resolution)
        {
            resolutions.A_1 = resolution;
            resolutions.A_2 = resolution;
            resolutions.B_1 = resolution;
            resolutions.B_2 = resolution;
            resolutions.C_1 = resolution;
            resolutions.C_2 = resolution;
            resolutions.DPB1_1 = resolution;
            resolutions.DPB1_2 = resolution;
            resolutions.DQB1_1 = resolution;
            resolutions.DQB1_2 = resolution;
            resolutions.DRB1_1 = resolution;
            resolutions.DRB1_2 = resolution;
            return this;
        }

        public HlaTypingCategorySetBuilder WithTypingResolutionAtLocus(Locus locus, HlaTypingResolution resolution)
        {
            resolutions.SetAtLocus(locus, TypePositions.Both, resolution);
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
                resolutions.SetAtLocus(resolution.Key, TypePositions.Both, resolution.Value);
            }

            return this;
        }

        public PhenotypeInfo<HlaTypingResolution> Build()
        {
            return resolutions;
        }
    }
}