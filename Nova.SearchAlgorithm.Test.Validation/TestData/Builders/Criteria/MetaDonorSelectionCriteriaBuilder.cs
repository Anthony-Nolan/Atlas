using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.PatientDataSelection;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Builders.Criteria
{
    public class MetaDonorSelectionCriteriaBuilder
    {
        private readonly MetaDonorSelectionCriteria criteria;

        public MetaDonorSelectionCriteriaBuilder()
        {
            criteria = new MetaDonorSelectionCriteria
            {
                MatchLevels = new PhenotypeInfo<MatchLevel>(),
                IsHomozygous = new LocusInfo<bool>(false),
                AlleleStringContainsDifferentAntigenGroups = new PhenotypeInfo<bool>(false),
                HasNonNullExpressionSuffix = new PhenotypeInfo<bool>(false)
            };
        }

        public MetaDonorSelectionCriteriaBuilder WithMatchingDonorType(DonorType donorType)
        {
            criteria.MatchingDonorType = donorType;
            return this;
        }

        public MetaDonorSelectionCriteriaBuilder WithMatchingRegistry(RegistryCode registry)
        {
            criteria.MatchingRegistry = registry;
            return this;
        }

        public MetaDonorSelectionCriteriaBuilder WithMatchLevelAtAllPositions(MatchLevel matchLevel)
        {
            criteria.MatchLevels = new PhenotypeInfo<MatchLevel>(matchLevel);
            return this;
        }

        public MetaDonorSelectionCriteriaBuilder WithTgsTypingCategoryAtAllPositions(TgsHlaTypingCategory typingCategory)
        {
            criteria.MatchingTgsTypingCategories = new PhenotypeInfo<TgsHlaTypingCategory>(typingCategory);
            return this;
        }

        public MetaDonorSelectionCriteriaBuilder WithDatabaseDonorSpecifications(IEnumerable<DatabaseDonorSpecification> databaseDonorSpecifications)
        {
            criteria.DatabaseDonorDetailsSets = databaseDonorSpecifications.ToList();
            return this;
        }

        public MetaDonorSelectionCriteriaBuilder HomozygousAtAllLoci()
        {
            criteria.IsHomozygous = new LocusInfo<bool>(true);
            return this;
        }

        public MetaDonorSelectionCriteriaBuilder ShouldContainDifferentAntigenGroupsAtAllPositions()
        {
            criteria.AlleleStringContainsDifferentAntigenGroups = new PhenotypeInfo<bool>(true);
            return this;
        }

        public MetaDonorSelectionCriteriaBuilder WithNumberOfDonorsToSkip(int numberOfDonors)
        {
            criteria.MetaDonorsToSkip = numberOfDonors;
            return this;
        }

        public MetaDonorSelectionCriteriaBuilder WithNonNullExpressionSuffixAt(Locus locus)
        {
            criteria.HasNonNullExpressionSuffix.SetAtLocus(locus, true);
            return this;
        }

        public MetaDonorSelectionCriteriaBuilder WithNullAlleleAtPosition(Locus locus, TypePosition position)
        {
            criteria.IsNullExpressing.SetAtPosition(locus, position, true);
            return this;
        }

        public MetaDonorSelectionCriteriaBuilder WithNullAlleleAtAllPositions()
        {
            return LocusSettings.AllLoci.Aggregate(this,
                (current, locus) => current.WithNullAlleleAtPosition(locus, TypePosition.One).WithNullAlleleAtPosition(locus, TypePosition.Two));
        }

        public MetaDonorSelectionCriteriaBuilder WithNonNullExpressionSuffixAtAllLoci()
        {
            return LocusSettings.AllLoci.Aggregate(this, (current, locus) => current.WithNonNullExpressionSuffixAt(locus));
        }

        public MetaDonorSelectionCriteria Build()
        {
            return criteria;
        }
    }
}