using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.PatientDataSelection;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Builders.Criteria
{
    public class PatientHlaSelectionCriteriaBuilder
    {
        private readonly PatientHlaSelectionCriteria criteria;

        public PatientHlaSelectionCriteriaBuilder()
        {
            criteria = new PatientHlaSelectionCriteria
            {
                HlaSources = new PhenotypeInfo<PatientHlaSource>(PatientHlaSource.Match),
                MatchLevels = new PhenotypeInfo<MatchLevel>(MatchLevel.Allele),
                IsHomozygous = new LocusInfo<bool>(false),
                Orientations = new LocusInfo<MatchOrientation>(MatchOrientation.Arbitrary),
            };
        }

        public PatientHlaSelectionCriteriaBuilder MatchingAtPosition(Locus locus, TypePosition position)
        {
            criteria.HlaSources.SetAtPosition(locus, position, PatientHlaSource.Match);
            return this;
        }

        public PatientHlaSelectionCriteriaBuilder MatchingAtBothPositions(Locus locus)
        {
            criteria.HlaSources.SetAtPosition(locus, TypePosition.One, PatientHlaSource.Match);
            criteria.HlaSources.SetAtPosition(locus, TypePosition.Two, PatientHlaSource.Match);
            return this;
        }

        public PatientHlaSelectionCriteriaBuilder NotMatchingAtPosition(Locus locus, TypePosition position)
        {
            criteria.HlaSources.SetAtPosition(locus, position, PatientHlaSource.ExpressingAlleleMismatch);
            return this;
        }

        public PatientHlaSelectionCriteriaBuilder NotMatchingAtEitherPosition(Locus locus)
        {
            criteria.HlaSources.SetAtPosition(locus, TypePosition.One, PatientHlaSource.ExpressingAlleleMismatch);
            criteria.HlaSources.SetAtPosition(locus, TypePosition.Two, PatientHlaSource.ExpressingAlleleMismatch);
            return this;
        }

        public PatientHlaSelectionCriteriaBuilder WithMatchLevelAtAllLoci(MatchLevel matchLevel)
        {
            criteria.MatchLevels = new PhenotypeInfo<MatchLevel>(matchLevel);
            return this;
        }

        public PatientHlaSelectionCriteriaBuilder HomozygousAtLocus(Locus locus)
        {
            criteria.IsHomozygous.SetAtLocus(locus, true);
            return this;
        }

        public PatientHlaSelectionCriteriaBuilder WithMatchOrientationAtLocus(Locus locus, MatchOrientation orientation)
        {
            criteria.Orientations.SetAtLocus(locus, orientation);
            return this;
        }

        public PatientHlaSelectionCriteriaBuilder WithHlaSourceAtPosition(Locus locus, TypePosition position, PatientHlaSource hlaSource)
        {
            criteria.HlaSources.SetAtPosition(locus, position, hlaSource);
            return this;
        }

        public PatientHlaSelectionCriteria Build()
        {
            return criteria;
        }
    }
}