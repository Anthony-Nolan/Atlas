using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Builders
{
    public class GenotypeCriteriaBuilder
    {
        private readonly GenotypeCriteria genotypeCriteria;

        public GenotypeCriteriaBuilder()
        {
            genotypeCriteria = new GenotypeCriteria
            {
                PGroupMatchPossible = new PhenotypeInfo<bool>(),
                GGroupMatchPossible = new PhenotypeInfo<bool>(),
                TgsHlaCategories = new PhenotypeInfo<TgsHlaTypingCategory>
                {
                    A_1 = TgsHlaTypingCategory.Arbitrary,
                    A_2 = TgsHlaTypingCategory.Arbitrary,
                    B_1 = TgsHlaTypingCategory.Arbitrary,
                    B_2 = TgsHlaTypingCategory.Arbitrary,
                    C_1 = TgsHlaTypingCategory.Arbitrary,
                    C_2 = TgsHlaTypingCategory.Arbitrary,
                    // There is no test data for DPB1 that is less than four-field
                    DPB1_1 = TgsHlaTypingCategory.FourFieldAllele,
                    DPB1_2 = TgsHlaTypingCategory.FourFieldAllele,
                    DQB1_1 = TgsHlaTypingCategory.Arbitrary,
                    DQB1_2 = TgsHlaTypingCategory.Arbitrary,
                    DRB1_1 = TgsHlaTypingCategory.Arbitrary,
                    DRB1_2 = TgsHlaTypingCategory.Arbitrary,
                }
            };
        }

        public GenotypeCriteriaBuilder WithTgsTypingCategoryAtAllLoci(TgsHlaTypingCategory category)
        {
            genotypeCriteria.TgsHlaCategories = new PhenotypeInfo<TgsHlaTypingCategory>
            {
                A_1 = category,
                A_2 = category,
                B_1 = category,
                B_2 = category,
                C_1 = category,
                C_2 = category,
                // There is no test data for DPB1 that is less than four-field
                DPB1_1 = TgsHlaTypingCategory.FourFieldAllele,
                DPB1_2 = TgsHlaTypingCategory.FourFieldAllele,
                DQB1_1 = category,
                DQB1_2 = category,
                DRB1_1 = category,
                DRB1_2 = category
            };

            return this;
        }
        
        public GenotypeCriteriaBuilder WithPGroupMatchPossibleAtAllLoci()
        {
            genotypeCriteria.PGroupMatchPossible = new PhenotypeInfo<bool>
            {
                A_1 = true,
                A_2 = true,
                B_1 = true,
                B_2 = true,
                C_1 = true,
                C_2 = true,
                DPB1_1 = true,
                DPB1_2 = true,
                DQB1_1 = true,
                DQB1_2 = true,
                DRB1_1 = true,
                DRB1_2 = true
            };
            
            return this;
        }
        
        public GenotypeCriteriaBuilder WithGGroupMatchPossibleAtAllLoci()
        {
            genotypeCriteria.GGroupMatchPossible = new PhenotypeInfo<bool>
            {
                A_1 = true,
                A_2 = true,
                B_1 = true,
                B_2 = true,
                C_1 = true,
                C_2 = true,
                DPB1_1 = true,
                DPB1_2 = true,
                DQB1_1 = true,
                DQB1_2 = true,
                DRB1_1 = true,
                DRB1_2 = true
            };
            
            return this;
        }
        
        public GenotypeCriteria Build()
        {
            return genotypeCriteria;
        }
    }
}