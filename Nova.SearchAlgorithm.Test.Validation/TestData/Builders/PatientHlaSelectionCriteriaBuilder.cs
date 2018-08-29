using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.PatientDataSelection;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Builders
{
    public class PatientHlaSelectionCriteriaBuilder
    {
        private readonly PatientHlaSelectionCriteria criteria;
        
        public PatientHlaSelectionCriteriaBuilder()
        {
            criteria = new PatientHlaSelectionCriteria
            {
                HlaMatches = new PhenotypeInfo<bool>(true),
                MatchLevels = new PhenotypeInfo<MatchLevel>(MatchLevel.Allele),
                IsHomozygous = new LocusInfo<bool>(false),
                Orientations = new LocusInfo<MatchOrientation>(MatchOrientation.Arbitrary),
            };
        }

        public PatientHlaSelectionCriteriaBuilder MatchingAtPosition(Locus locus, TypePositions positions)
        {
            criteria.HlaMatches.SetAtPosition(locus, positions, true);
            return this;
        }
        
        public PatientHlaSelectionCriteriaBuilder NotMatchingAtPosition(Locus locus, TypePositions positions)
        {
            criteria.HlaMatches.SetAtPosition(locus, positions, false);
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
        
        public PatientHlaSelectionCriteria Build()
        {
            return criteria;
        }
    }
}