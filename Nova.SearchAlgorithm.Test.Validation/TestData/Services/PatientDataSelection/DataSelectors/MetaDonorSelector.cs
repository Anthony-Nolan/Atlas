using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Test.Validation.TestData.Exceptions;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.PatientDataSelection;
using Nova.SearchAlgorithm.Test.Validation.TestData.Repositories;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection
{
    public interface IMetaDonorSelector
    {
        /// <summary>
        /// Will return a meta-donor matching the criteria from the available test data.
        /// </summary>
        MetaDonor GetMetaDonor(MetaDonorSelectionCriteria criteria);
    }

    public class MetaDonorSelector : IMetaDonorSelector
    {
        private readonly IMetaDonorRepository metaDonorRepository;

        public MetaDonorSelector(IMetaDonorRepository metaDonorRepository)
        {
            this.metaDonorRepository = metaDonorRepository;
        }

        public MetaDonor GetMetaDonor(MetaDonorSelectionCriteria criteria)
        {
            var matchingMetaDonors = metaDonorRepository.AllMetaDonors()
                .Where(md => FulfilsDonorInfoCriteria(criteria, md) && FulfilsDonorHlaCriteria(criteria, md))
                .ToList();

            if (!matchingMetaDonors.Any())
            {
                throw new MetaDonorNotFoundException("No meta-donors found matching specified criteria.");
            }

            var newMetaDonors = matchingMetaDonors.Skip(criteria.MetaDonorsToSkip).ToList();

            if (!newMetaDonors.Any())
            {
                throw new MetaDonorNotFoundException(
                    $"No more meta-donors found matching specified criteria. Ignored {criteria.MetaDonorsToSkip} meta-donors. Is there enough test data?");
            }

            var metaDonor = newMetaDonors.First();
            return metaDonor;
        }

        private static bool FulfilsDonorInfoCriteria(MetaDonorSelectionCriteria criteria, MetaDonor metaDonor)
        {
            return FulfilsDonorTypeCriteria(criteria, metaDonor)
                   && FulfilsRegistryCriteria(criteria, metaDonor);
        }

        private static bool FulfilsDonorHlaCriteria(MetaDonorSelectionCriteria criteria, MetaDonor metaDonor)
        {
            return FulfilsTgsTypingCategoryCriteria(criteria, metaDonor)
                   && FulfilsHomozygousCriteria(criteria, metaDonor)
                   && FulfilsMatchLevelCriteria(criteria, metaDonor)
                   && FulfilsTypingResolutionCriteria(criteria, metaDonor);
        }

        private static bool FulfilsHomozygousCriteria(MetaDonorSelectionCriteria criteria, MetaDonor metaDonor)
        {
            var perLocusFulfilment = criteria.IsHomozygous.Map((locus, shouldBeHomozygous) =>
            {
                if (shouldBeHomozygous)
                {
                    return metaDonor.GenotypeCriteria.IsHomozygous.DataAtLocus(locus);
                }

                // If we don't explicitly need a homozygous donor, we don't mind whether the donor is homozygous or not
                return true;
            });

            return perLocusFulfilment.ToEnumerable().All(x => x);
        }

        private static bool FulfilsTypingResolutionCriteria(MetaDonorSelectionCriteria criteria, MetaDonor metaDonor)
        {
            return criteria.TypingResolutionSets.All(resolutionSet => metaDonor.HlaTypingResolutionSets.Any(resolutionSet.Equals));
        }

        private static bool FulfilsMatchLevelCriteria(MetaDonorSelectionCriteria criteria, MetaDonor metaDonor)
        {
            // Maps to a list of booleans - each one indicates whether the criteria are met at that locus/position
            return criteria.MatchLevels.Map((l, p, matchLevel) =>
            {
                switch (matchLevel)
                {
                    case MatchLevel.Allele:
                        return true;
                    case MatchLevel.FirstThreeFieldAllele:
                        return metaDonor.GenotypeCriteria.ThreeFieldMatchPossible.DataAtPosition(l, p);
                    case MatchLevel.PGroup:
                        return metaDonor.GenotypeCriteria.PGroupMatchPossible.DataAtPosition(l, p);
                    case MatchLevel.GGroup:
                        return metaDonor.GenotypeCriteria.GGroupMatchPossible.DataAtPosition(l, p);
                    case MatchLevel.FirstTwoFieldAllele:
                        return metaDonor.GenotypeCriteria.TwoFieldMatchPossible.DataAtPosition(l, p);
                    default:
                        throw new ArgumentOutOfRangeException(nameof(matchLevel), matchLevel, null);
                }
            }).ToEnumerable().All(x => x);
        }

        private static bool FulfilsTgsTypingCategoryCriteria(MetaDonorSelectionCriteria criteria, MetaDonor metaDonor)
        {
            return criteria.MatchingTgsTypingCategories.Equals(metaDonor.GenotypeCriteria.TgsHlaCategories);
        }

        private static bool FulfilsRegistryCriteria(MetaDonorSelectionCriteria criteria, MetaDonor metaDonor)
        {
            return criteria.MatchingRegistry == metaDonor.Registry;
        }

        private static bool FulfilsDonorTypeCriteria(MetaDonorSelectionCriteria criteria, MetaDonor metaDonor)
        {
            return criteria.MatchingDonorType == metaDonor.DonorType;
        }
    }
}