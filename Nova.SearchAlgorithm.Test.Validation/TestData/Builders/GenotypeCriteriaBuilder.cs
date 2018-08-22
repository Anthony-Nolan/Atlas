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
                PGroupMatchPossible = new PhenotypeInfo<bool>(false),
                GGroupMatchPossible = new PhenotypeInfo<bool>(false),
                ThreeFieldMatchPossible = new PhenotypeInfo<bool>(false),
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
                },
                IsHomozygous = new LocusInfo<bool>(false),
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
            genotypeCriteria.PGroupMatchPossible = new PhenotypeInfo<bool>(true);
            return this;
        }
        
        public GenotypeCriteriaBuilder WithGGroupMatchPossibleAtAllLoci()
        {
            genotypeCriteria.GGroupMatchPossible = new PhenotypeInfo<bool>(true);
            return this;
        }
        
        public GenotypeCriteriaBuilder WithThreeFieldMatchPossibleAtAllLoci()
        {
            genotypeCriteria.ThreeFieldMatchPossible = new PhenotypeInfo<bool>(true);
            return this;
        }

        public GenotypeCriteriaBuilder HomozygousAtLocus(Locus locus)
        {
            genotypeCriteria.IsHomozygous.SetAtLocus(locus, true);
            return this;
        }
        
        public GenotypeCriteria Build()
        {
            return genotypeCriteria;
        }
    }
}