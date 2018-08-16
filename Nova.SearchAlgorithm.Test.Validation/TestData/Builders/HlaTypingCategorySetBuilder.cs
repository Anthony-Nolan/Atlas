using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;

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
        
        public HlaTypingCategorySetBuilder UntypedAtLocus(Locus locus)
        {
            categories.SetAtLocus(locus, TypePositions.Both, HlaTypingResolution.Untyped);
            return this;
        }

        public PhenotypeInfo<HlaTypingResolution> Build()
        {
            return categories;
        }
    }
}