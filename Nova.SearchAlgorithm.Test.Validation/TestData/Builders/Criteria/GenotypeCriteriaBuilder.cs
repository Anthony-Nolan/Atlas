using System;
using System.Linq;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Exceptions;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Builders.Criteria
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
        
        public GenotypeCriteriaBuilder WithNullAlleleAtAllLoci()
        {
            genotypeCriteria.AlleleSources = new PhenotypeInfo<Dataset>(Dataset.NullAlleles);
            return this;
        }
        
        public GenotypeCriteriaBuilder WithNullAlleleAtPosition(Locus locus, TypePosition position)
        {
            genotypeCriteria.AlleleSources.SetAtPosition(locus, position, Dataset.NullAlleles);
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

        public GenotypeCriteriaBuilder WithMatchLevelPossibleAtLocus(MatchLevel matchLevel, Locus locus)
        {
            switch (matchLevel)
            {
                case MatchLevel.Allele:
                    genotypeCriteria.AlleleSources.SetAtLocus(locus, Dataset.TgsAlleles);
                    break;
                case MatchLevel.FirstThreeFieldAllele:
                    genotypeCriteria.AlleleSources.SetAtLocus(locus, Dataset.FourFieldAllelesWithThreeFieldMatchPossible);
                    break;
                case MatchLevel.FirstTwoFieldAllele:           
                    genotypeCriteria.AlleleSources.SetAtLocus(locus, Dataset.ThreeFieldAllelesWithTwoFieldMatchPossible);
                    break;
                case MatchLevel.PGroup:
                    genotypeCriteria.AlleleSources.SetAtLocus(locus, Dataset.PGroupMatchPossible);
                    break;
                case MatchLevel.GGroup:
                    genotypeCriteria.AlleleSources.SetAtLocus(locus, Dataset.GGroupMatchPossible);
                    break;
                case MatchLevel.Protein:
                    genotypeCriteria.AlleleSources.SetAtLocus(locus, Dataset.ProteinMatchPossible);
                    break;
                case MatchLevel.CDna:
                    genotypeCriteria.AlleleSources.SetAtLocus(locus, Dataset.CDnaMatchPossible);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(matchLevel), matchLevel, null);
            }

            return this;
        }

        public GenotypeCriteriaBuilder WithMatchLevelPossibleAtAllLoci(MatchLevel matchLevel)
        {
            return LocusConfig.AllLoci().Aggregate(this, (current, locus) => current.WithMatchLevelPossibleAtLocus(matchLevel, locus));
        }

        public GenotypeCriteriaBuilder WithStringOfSingleAndMultiplePGroupsPossibleAtAllLoci()
        {
            genotypeCriteria.AlleleSources = new PhenotypeInfo<Dataset>(Dataset.AllelesWithStringsOfSingleAndMultiplePGroupsPossible);
            return this;
        }
        
        public GenotypeCriteria Build()
        {
            return genotypeCriteria;
        }
    }
}