using System;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Exceptions;
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
                AlleleSources = new PhenotypeInfo<Dataset>(Dataset.TgsAlleles),
                IsHomozygous = new LocusInfo<bool>(false),
                AlleleStringContainsDifferentAntigenGroups = new PhenotypeInfo<bool>(false),
            };
        }

        public GenotypeCriteriaBuilder WithTgsTypingCategoryAtAllLoci(TgsHlaTypingCategory category)
        {
            switch (category)
            {
                case TgsHlaTypingCategory.FourFieldAllele:
                    genotypeCriteria.AlleleSources = new PhenotypeInfo<Dataset>(Dataset.FourFieldTgsAlleles);
                    break;
                case TgsHlaTypingCategory.ThreeFieldAllele:
                    genotypeCriteria.AlleleSources = new PhenotypeInfo<Dataset>(Dataset.ThreeFieldTgsAlleles);
                    break;
                case TgsHlaTypingCategory.TwoFieldAllele:
                    genotypeCriteria.AlleleSources = new PhenotypeInfo<Dataset>(Dataset.TwoFieldTgsAlleles);
                    break;
                case TgsHlaTypingCategory.Arbitrary:
                    genotypeCriteria.AlleleSources = new PhenotypeInfo<Dataset>(Dataset.TgsAlleles);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(category), category, null);
            }
            return this;
        }
        
        public GenotypeCriteriaBuilder WithAlleleStringOfSubtypesPossibleAtAllLoci()
        {
            genotypeCriteria.AlleleSources = new PhenotypeInfo<Dataset>(Dataset.AlleleStringOfSubtypesPossible);
            return this;
        }
        
        public GenotypeCriteriaBuilder WithPGroupMatchPossibleAtAllLoci()
        {
            genotypeCriteria.AlleleSources = new PhenotypeInfo<Dataset>(Dataset.PGroupMatchPossible);
            return this;
        }
        
        public GenotypeCriteriaBuilder WithGGroupMatchPossibleAtAllLoci()
        {
            genotypeCriteria.AlleleSources = new PhenotypeInfo<Dataset>(Dataset.GGroupMatchPossible);
            return this;
        }
        
        public GenotypeCriteriaBuilder WithThreeFieldMatchPossibleAtAllLoci()
        {
            genotypeCriteria.AlleleSources = new PhenotypeInfo<Dataset>(Dataset.FourFieldAllelesWithThreeFieldMatchPossible);
            return this;
        }
        
        public GenotypeCriteriaBuilder WithTwoFieldMatchPossibleAtAllLoci()
        {
            genotypeCriteria.AlleleSources = new PhenotypeInfo<Dataset>(Dataset.ThreeFieldAllelesWithTwoFieldMatchPossible);
            return this;
        }
        
        public GenotypeCriteriaBuilder WithNullAlleleAtAllLoci()
        {
            genotypeCriteria.AlleleSources = new PhenotypeInfo<Dataset>(Dataset.NullAlleles);
            return this;
        }

        public GenotypeCriteriaBuilder WithNonNullExpressionSuffixAtLocus(Locus locus)
        {
            if (locus == Locus.Drb1)
            {
                throw new InvalidTestDataException("No test data exists with a non-null expression suffix at DRB1");
            }
            genotypeCriteria.AlleleSources.SetAtLocus(locus, Dataset.AllelesWithNonNullExpressionSuffix);
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

        public GenotypeCriteriaBuilder WithAlleleStringContainingDifferentGroupsAtAllLoci()
        {
            genotypeCriteria.AlleleStringContainsDifferentAntigenGroups = new PhenotypeInfo<bool>(true);
            return this;
        }

        public GenotypeCriteriaBuilder WithCDnaMatchPossibleAtAllLoci()
        {
            genotypeCriteria.AlleleSources = new PhenotypeInfo<Dataset>(Dataset.CDnaMatchPossible);
            return this;
        }

        public GenotypeCriteria Build()
        {
            return genotypeCriteria;
        }
    }
}