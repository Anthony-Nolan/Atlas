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
                TgsHlaCategories = new PhenotypeInfo<TgsHlaTypingCategory>(TgsHlaTypingCategory.Arbitrary),
                IsHomozygous = new LocusInfo<bool>(false),
            };
        }

        public GenotypeCriteriaBuilder WithTgsTypingCategoryAtAllLoci(TgsHlaTypingCategory category)
        {
            genotypeCriteria.TgsHlaCategories = new PhenotypeInfo<TgsHlaTypingCategory>(category);
            return this;
        }
        
        /// <summary>
        /// Option to exlucde DPB1, as it is a special case in some scenarios.
        /// This is because test data is incomplete, e.g. two field DPB1 alleles have no serology data
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