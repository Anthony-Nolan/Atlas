using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.Hla;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.PatientDataSelection;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Resources.SpecificTestCases;
using static EnumStringValues.EnumExtensions;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Builders.Criteria
{
    public class DatabaseDonorSelectionCriteriaBuilder
    {
        private readonly DatabaseDonorSpecification criteria;

        public DatabaseDonorSelectionCriteriaBuilder()
        {
            criteria = new DatabaseDonorSpecification();
        }
        
        public DatabaseDonorSelectionCriteriaBuilder WithAllLociAtTypingResolution(HlaTypingResolution resolution)
        {
            foreach (var locus in EnumerateValues<Locus>())
            {
                criteria.MatchingTypingResolutions.SetAtLocus(locus, resolution);
            }
            return this;
        }

        public DatabaseDonorSelectionCriteriaBuilder WithTypingResolutionAtLocus(Locus locus, HlaTypingResolution resolution)
        {
            criteria.MatchingTypingResolutions.SetAtLocus(locus, resolution);
            return this;
        }

        public DatabaseDonorSelectionCriteriaBuilder WithNonGenotypeAlleleAtLocus(Locus locus)
        {
            criteria.ShouldMatchGenotype.SetAtLocus(locus, false);
            return this;
        }

        public DatabaseDonorSelectionCriteriaBuilder WithNonGenotypeAlleleAtPosition(Locus locus, TypePosition position)
        {
            criteria.ShouldMatchGenotype.SetAtPosition(locus, position, false);
            return this;
        }

        public DatabaseDonorSelectionCriteriaBuilder UntypedAtLocus(Locus locus)
        {
            return WithTypingResolutionAtLocus(locus, HlaTypingResolution.Untyped);
        }

        public DatabaseDonorSelectionCriteriaBuilder WithDifferentlyTypedLoci()
        {
            foreach (var resolution in TestCaseTypingResolutions.DifferentLociResolutions)
            {
                criteria.MatchingTypingResolutions.SetAtLocus(resolution.Key, resolution.Value);
            }

            return this;
        }

        public DatabaseDonorSpecification Build()
        {
            return criteria;
        }
    }
}