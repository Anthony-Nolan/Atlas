using System;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
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
            genotypeCriteria.AlleleSources = genotypeCriteria.AlleleSources.SetPosition(locus, position, Dataset.NullAlleles);
            return this;
        }

        public GenotypeCriteriaBuilder WithNonNullExpressionSuffixAtLocus(Locus locus)
        {
            if (locus == Locus.Drb1)
            {
                throw new InvalidTestDataException("No test data exists with a non-null expression suffix at DRB1");
            }

            genotypeCriteria.AlleleSources = genotypeCriteria.AlleleSources =
                genotypeCriteria.AlleleSources.SetLocus(locus, Dataset.AllelesWithNonNullExpressionSuffix);
            return this;
        }

        public GenotypeCriteriaBuilder HomozygousAtLocus(Locus locus)
        {
            genotypeCriteria.IsHomozygous = genotypeCriteria.IsHomozygous = genotypeCriteria.IsHomozygous.SetLocus(locus, true);
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
            genotypeCriteria.AlleleSources = matchLevel switch
            {
                MatchLevel.Allele => genotypeCriteria.AlleleSources = genotypeCriteria.AlleleSources.SetLocus(locus, Dataset.TgsAlleles),
                MatchLevel.FirstThreeFieldAllele => genotypeCriteria.AlleleSources = genotypeCriteria.AlleleSources.SetLocus(
                    locus,
                    Dataset.FourFieldAllelesWithThreeFieldMatchPossible
                ),
                MatchLevel.FirstTwoFieldAllele => genotypeCriteria.AlleleSources =
                    genotypeCriteria.AlleleSources.SetLocus(locus, Dataset.ThreeFieldAllelesWithTwoFieldMatchPossible),
                MatchLevel.PGroup => genotypeCriteria.AlleleSources = genotypeCriteria.AlleleSources.SetLocus(locus, Dataset.PGroupMatchPossible),
                MatchLevel.GGroup => genotypeCriteria.AlleleSources = genotypeCriteria.AlleleSources.SetLocus(locus, Dataset.GGroupMatchPossible),
                MatchLevel.Protein => genotypeCriteria.AlleleSources = genotypeCriteria.AlleleSources.SetLocus(locus, Dataset.ProteinMatchPossible),
                MatchLevel.CDna => genotypeCriteria.AlleleSources = genotypeCriteria.AlleleSources.SetLocus(locus, Dataset.CDnaMatchPossible),
                _ => throw new ArgumentOutOfRangeException(nameof(matchLevel), matchLevel, null)
            };

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