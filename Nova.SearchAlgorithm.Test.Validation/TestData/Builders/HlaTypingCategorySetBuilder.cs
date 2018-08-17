using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;
using Nova.SearchAlgorithm.Test.Validation.TestData.Resources.SpecificTestCases;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Builders
{
    public class HlaTypingCategorySetBuilder
    {
        private readonly PhenotypeInfo<HlaTypingResolution> categories;

        public HlaTypingCategorySetBuilder()
        {
            categories = new PhenotypeInfo<HlaTypingResolution>
            {
                A_1 = HlaTypingResolution.Tgs,
                A_2 = HlaTypingResolution.Tgs,
                B_1 = HlaTypingResolution.Tgs,
                B_2 = HlaTypingResolution.Tgs,
                C_1 = HlaTypingResolution.Tgs,
                C_2 = HlaTypingResolution.Tgs,
                DPB1_1 = HlaTypingResolution.Tgs,
                DPB1_2 = HlaTypingResolution.Tgs,
                DQB1_1 = HlaTypingResolution.Tgs,
                DQB1_2 = HlaTypingResolution.Tgs,
                DRB1_1 = HlaTypingResolution.Tgs,
                DRB1_2 = HlaTypingResolution.Tgs,
            };
        }

        public HlaTypingCategorySetBuilder WithAllLociAtTypingCategory(HlaTypingResolution resolution)
        {
            categories.A_1 = resolution;
            categories.A_2 = resolution;
            categories.B_1 = resolution;
            categories.B_2 = resolution;
            categories.C_1 = resolution;
            categories.C_2 = resolution;
            categories.DPB1_1 = resolution;
            categories.DPB1_2 = resolution;
            categories.DQB1_1 = resolution;
            categories.DQB1_2 = resolution;
            categories.DRB1_1 = resolution;
            categories.DRB1_2 = resolution;
            return this;
        }

        public HlaTypingCategorySetBuilder WithTypingResolutionAtLocus(Locus locus, HlaTypingResolution resolution)
        {
            categories.SetAtLocus(locus, TypePositions.Both, resolution);
            return this;
        }

        public HlaTypingCategorySetBuilder UntypedAtLocus(Locus locus)
        {
            return this.WithTypingResolutionAtLocus(locus, HlaTypingResolution.Untyped);
        }

        public HlaTypingCategorySetBuilder WithDifferentlyTypedLoci()
        {
            foreach (var resolution in TestCaseTypingResolutions.DifferentLociResolutions)
            {
                categories.SetAtLocus(resolution.Key, TypePositions.Both, resolution.Value);
            }

            return this;
        }

        public PhenotypeInfo<HlaTypingResolution> Build()
        {
            return categories;
        }
    }
}