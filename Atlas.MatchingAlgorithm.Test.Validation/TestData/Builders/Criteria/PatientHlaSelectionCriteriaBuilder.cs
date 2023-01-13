using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
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
                IsHomozygous = new LociInfo<bool>(false),
                Orientations = new LociInfo<MatchOrientation>(MatchOrientation.Arbitrary),
            };
        }

        public PatientHlaSelectionCriteriaBuilder MatchingAtPosition(Locus locus, LocusPosition position)
        {
            criteria.HlaSources = criteria.HlaSources.SetPosition(locus, position, PatientHlaSource.Match);
            return this;
        }

        public PatientHlaSelectionCriteriaBuilder MatchingAtBothPositions(Locus locus)
        {
            criteria.HlaSources = criteria.HlaSources.SetPosition(locus, LocusPosition.One, PatientHlaSource.Match);
            criteria.HlaSources = criteria.HlaSources.SetPosition(locus, LocusPosition.Two, PatientHlaSource.Match);
            return this;
        }

        public PatientHlaSelectionCriteriaBuilder NotMatchingAtPosition(Locus locus, LocusPosition position)
        {
            criteria.HlaSources = criteria.HlaSources.SetPosition(locus, position, PatientHlaSource.ExpressingAlleleMismatch);
            return this;
        }

        public PatientHlaSelectionCriteriaBuilder NotMatchingAtEitherPosition(Locus locus)
        {
            criteria.HlaSources = criteria.HlaSources.SetPosition(locus, LocusPosition.One, PatientHlaSource.ExpressingAlleleMismatch);
            criteria.HlaSources = criteria.HlaSources.SetPosition(locus, LocusPosition.Two, PatientHlaSource.ExpressingAlleleMismatch);
            return this;
        }

        public PatientHlaSelectionCriteriaBuilder WithMatchLevelAtAllLoci(MatchLevel matchLevel)
        {
            criteria.MatchLevels = new PhenotypeInfo<MatchLevel>(matchLevel);
            return this;
        }

        public PatientHlaSelectionCriteriaBuilder HomozygousAtLocus(Locus locus)
        {
            criteria.IsHomozygous = criteria.IsHomozygous.SetLocus(locus, true);
            return this;
        }

        public PatientHlaSelectionCriteriaBuilder WithMatchOrientationAtLocus(Locus locus, MatchOrientation orientation)
        {
            criteria.Orientations = criteria.Orientations.SetLocus(locus, orientation);
            return this;
        }

        public PatientHlaSelectionCriteriaBuilder WithHlaSourceAtPosition(Locus locus, LocusPosition position, PatientHlaSource hlaSource)
        {
            criteria.HlaSources = criteria.HlaSources.SetPosition(locus, position, hlaSource);
            return this;
        }

        public PatientHlaSelectionCriteria Build()
        {
            return criteria;
        }
    }
}