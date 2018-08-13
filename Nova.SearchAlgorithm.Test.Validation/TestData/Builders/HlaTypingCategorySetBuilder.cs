using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Builders
{
    public class HlaTypingCategorySetBuilder
    {
        private readonly PhenotypeInfo<HlaTypingCategory> categories;

        public HlaTypingCategorySetBuilder()
        {
            categories = new PhenotypeInfo<HlaTypingCategory>
            {
                A_1 = HlaTypingCategory.TgsFourFieldAllele,
                A_2 = HlaTypingCategory.TgsFourFieldAllele,
                B_1 = HlaTypingCategory.TgsFourFieldAllele,
                B_2 = HlaTypingCategory.TgsFourFieldAllele,
                C_1 = HlaTypingCategory.TgsFourFieldAllele,
                C_2 = HlaTypingCategory.TgsFourFieldAllele,
                DPB1_1 = HlaTypingCategory.TgsFourFieldAllele,
                DPB1_2 = HlaTypingCategory.TgsFourFieldAllele,
                DQB1_1 = HlaTypingCategory.TgsFourFieldAllele,
                DQB1_2 = HlaTypingCategory.TgsFourFieldAllele,
                DRB1_1 = HlaTypingCategory.TgsFourFieldAllele,
                DRB1_2 = HlaTypingCategory.TgsFourFieldAllele,
            };
        }

        public HlaTypingCategorySetBuilder WithAllLociAtTypingCategory(HlaTypingCategory category)
        {
            categories.A_1 = category;
            categories.A_2 = category;
            categories.B_1 = category;
            categories.B_2 = category;
            categories.C_1 = category;
            categories.C_2 = category;
            categories.DPB1_1 = category;
            categories.DPB1_2 = category;
            categories.DQB1_1 = category;
            categories.DQB1_2 = category;
            categories.DRB1_1 = category;
            categories.DRB1_2 = category;
            return this;
        }
        
        public HlaTypingCategorySetBuilder UntypedAtLocus(Locus locus)
        {
            categories.SetAtLocus(locus, TypePositions.Both, HlaTypingCategory.Untyped);
            return this;
        }

        public PhenotypeInfo<HlaTypingCategory> Build()
        {
            return categories;
        }
    }
}