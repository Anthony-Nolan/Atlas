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
                TwoFieldMatchPossible = new PhenotypeInfo<bool>(false),
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

        /// <summary>
        /// DPB1 is excluded, as it is a special case in most scenarios.
        /// This is because no standard test data is available for DPB1 as 2/3 field alleles
        /// </summary>
        public GenotypeCriteriaBuilder WithTgsTypingCategoryAtAllLociExceptDpb1(TgsHlaTypingCategory category)
        {
            genotypeCriteria.TgsHlaCategories.SetAtLocus(Locus.A, TypePositions.Both, category);
            genotypeCriteria.TgsHlaCategories.SetAtLocus(Locus.B, TypePositions.Both, category);
            genotypeCriteria.TgsHlaCategories.SetAtLocus(Locus.C, TypePositions.Both, category);
            genotypeCriteria.TgsHlaCategories.SetAtLocus(Locus.Dqb1, TypePositions.Both, category);
            genotypeCriteria.TgsHlaCategories.SetAtLocus(Locus.Drb1, TypePositions.Both, category);
            return this;
        }

        public GenotypeCriteriaBuilder WithTgsTypingCategoryAtLocus(Locus locus, TgsHlaTypingCategory category)
        {
            genotypeCriteria.TgsHlaCategories.SetAtLocus(locus, TypePositions.Both, category);
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
        
        public GenotypeCriteriaBuilder WithTwoFieldMatchPossibleAtAllLoci()
        {
            genotypeCriteria.TwoFieldMatchPossible = new PhenotypeInfo<bool>(true);
            return this;
        }

        public GenotypeCriteriaBuilder HomozygousAtLocus(Locus locus)
        {
            genotypeCriteria.IsHomozygous.SetAtLocus(locus, true);
            return this;
        }
        
        public GenotypeCriteriaBuilder HomozygousAtAllLoci()
        {
            genotypeCriteria.IsHomozygous = new LocusInfo<bool>(true);
            return this;
        }
        
        public GenotypeCriteria Build()
        {
            return genotypeCriteria;
        }
    }
}