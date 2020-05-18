using System;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Exceptions;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.Hla;
using static EnumStringValues.EnumExtensions;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Builders.Criteria
{
    public class GenotypeCriteriaBuilder
    {
        private readonly GenotypeCriteria genotypeCriteria;

        public GenotypeCriteriaBuilder()
        {
            genotypeCriteria = new GenotypeCriteria
            {
                AlleleSources = new PhenotypeInfo<Dataset>(Dataset.TgsAlleles),
                IsHomozygous = new LociInfo<bool>(false),
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
        
        public GenotypeCriteriaBuilder WithNullAlleleAtPosition(Locus locus, LocusPosition position)
        {
            genotypeCriteria.AlleleSources.SetPosition(locus, position, Dataset.NullAlleles);
            return this;
        }

        public GenotypeCriteriaBuilder WithNonNullExpressionSuffixAtLocus(Locus locus)
        {
            if (locus == Locus.Drb1)
            {
                throw new InvalidTestDataException("No test data exists with a non-null expression suffix at DRB1");
            }
            genotypeCriteria.AlleleSources.SetLocus(locus, Dataset.AllelesWithNonNullExpressionSuffix);
            return this;
        }

        public GenotypeCriteriaBuilder HomozygousAtLocus(Locus locus)
        {
            genotypeCriteria.IsHomozygous.SetLocus(locus, true);
            return this;
        }
        
        public GenotypeCriteriaBuilder HomozygousAtAllLoci()
        {
            genotypeCriteria.IsHomozygous = new LociInfo<bool>(true);
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
                    genotypeCriteria.AlleleSources.SetLocus(locus, Dataset.TgsAlleles);
                    break;
                case MatchLevel.FirstThreeFieldAllele:
                    genotypeCriteria.AlleleSources.SetLocus(locus, Dataset.FourFieldAllelesWithThreeFieldMatchPossible);
                    break;
                case MatchLevel.FirstTwoFieldAllele:           
                    genotypeCriteria.AlleleSources.SetLocus(locus, Dataset.ThreeFieldAllelesWithTwoFieldMatchPossible);
                    break;
                case MatchLevel.PGroup:
                    genotypeCriteria.AlleleSources.SetLocus(locus, Dataset.PGroupMatchPossible);
                    break;
                case MatchLevel.GGroup:
                    genotypeCriteria.AlleleSources.SetLocus(locus, Dataset.GGroupMatchPossible);
                    break;
                case MatchLevel.Protein:
                    genotypeCriteria.AlleleSources.SetLocus(locus, Dataset.ProteinMatchPossible);
                    break;
                case MatchLevel.CDna:
                    genotypeCriteria.AlleleSources.SetLocus(locus, Dataset.CDnaMatchPossible);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(matchLevel), matchLevel, null);
            }

            return this;
        }

        public GenotypeCriteriaBuilder WithMatchLevelPossibleAtAllLoci(MatchLevel matchLevel)
        {
            return EnumerateValues<Locus>().Aggregate(this, (current, locus) => current.WithMatchLevelPossibleAtLocus(matchLevel, locus));
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