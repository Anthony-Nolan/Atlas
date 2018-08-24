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
                HlaMatches = new PhenotypeInfo<bool>().Map((l, p, noop) => true),
                MatchLevels = new PhenotypeInfo<bool>().Map((l, p, noop) => MatchLevel.Allele),
                IsHomozygous = new LocusInfo<bool>().Map((l, noop) => false),
            };
        }

        public PatientHlaSelectionCriteriaBuilder MatchingAtPosition(Locus locus, TypePositions positions)
        {
            criteria.HlaMatches.SetAtLocus(locus, positions, true);
            return this;
        }
        
        public PatientHlaSelectionCriteriaBuilder NotMatchingAtPosition(Locus locus, TypePositions positions)
        {
            criteria.HlaMatches.SetAtLocus(locus, positions, false);
            return this;
        }
        
        public PatientHlaSelectionCriteriaBuilder WithMatchLevelAtAllLoci(MatchLevel matchLevel)
        {
            criteria.MatchLevels = new PhenotypeInfo<bool>().Map((l, p, noop) => matchLevel);
            return this;
        }

        public PatientHlaSelectionCriteriaBuilder HomozygousAtLocus(Locus locus)
        {
            criteria.IsHomozygous.SetAtLocus(locus, true);
            return this;
        }
        
        public PatientHlaSelectionCriteria Build()
        {
            return criteria;
        }
    }
}