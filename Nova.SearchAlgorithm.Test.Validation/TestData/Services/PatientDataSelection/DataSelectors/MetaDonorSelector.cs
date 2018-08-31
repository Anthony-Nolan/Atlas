using System;
using System.Linq;
using Nova.SearchAlgorithm.Test.Validation.TestData.Exceptions;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;
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
            return FulfilsHomozygousCriteria(criteria, metaDonor)
                   && FulfilsExpressionSuffixCriteria(criteria, metaDonor)
                   && FulfilsAlleleStringCriteria(criteria, metaDonor)
                   && FulfilsDatasetCriteria(criteria, metaDonor)
                   && FulfilsDatabaseDonorCriteria(criteria, metaDonor);
        }

        private static bool FulfilsExpressionSuffixCriteria(MetaDonorSelectionCriteria criteria, MetaDonor metaDonor)
        {
            var perLocusFulfilment = criteria.HasNonNullExpressionSuffix.Map((locus, position, shouldHaveSuffix) =>
                !shouldHaveSuffix
                || metaDonor.GenotypeCriteria.AlleleSources.DataAtPosition(locus, position) == Dataset.AllelesWithNonNullExpressionSuffix);

            return perLocusFulfilment.ToEnumerable().All(x => x);
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

        private static bool FulfilsAlleleStringCriteria(MetaDonorSelectionCriteria criteria, MetaDonor metaDonor)
        {
            var perLocusFulfilment = criteria.AlleleStringContainsDifferentAntigenGroups.Map((locus, position, shouldHaveDifferentAlleleGroups) =>
            {
                if (shouldHaveDifferentAlleleGroups)
                {
                    return metaDonor.GenotypeCriteria.AlleleStringContainsDifferentAntigenGroups.DataAtPosition(locus, position);
                }

                // If we don't explicitly need a donor with different allele groups in it's allele string representation,
                // we don't mind whether the donor fulfils this criteria or not
                return true;
            });

            return perLocusFulfilment.ToEnumerable().All(x => x);
        }

        private static bool FulfilsDatabaseDonorCriteria(MetaDonorSelectionCriteria criteria, MetaDonor metaDonor)
        {
            return criteria.DatabaseDonorDetailsSets.All(donorDetails =>
                metaDonor.DatabaseDonorSpecifications.Any(d => donorDetails == d)
            );
        }

        private static bool FulfilsDatasetCriteria(MetaDonorSelectionCriteria criteria, MetaDonor metaDonor)
        {
            // Maps to a list of booleans - each one indicates whether the criteria are met at that locus/position
            return metaDonor.GenotypeCriteria.AlleleSources.Map((l, p, dataset) =>
            {
                var tgsTypingRequired = criteria.MatchingTgsTypingCategories.DataAtPosition(l, p);
                // TODO: NOVA-1684: Ensure *all* requested match grades present
                var matchLevelRequired = criteria.MatchLevels.Single().DataAtPosition(l, p);

                switch (dataset)
                {
                    case Dataset.FourFieldTgsAlleles:
                        return matchLevelRequired == MatchLevel.Allele && tgsTypingRequired == TgsHlaTypingCategory.FourFieldAllele;
                    case Dataset.ThreeFieldTgsAlleles:
                        return matchLevelRequired == MatchLevel.Allele && tgsTypingRequired == TgsHlaTypingCategory.ThreeFieldAllele;
                    case Dataset.TwoFieldTgsAlleles:
                        return matchLevelRequired == MatchLevel.Allele && tgsTypingRequired == TgsHlaTypingCategory.TwoFieldAllele;
                    case Dataset.TgsAlleles:
                        return matchLevelRequired == MatchLevel.Allele && tgsTypingRequired == TgsHlaTypingCategory.Arbitrary;
                    case Dataset.PGroupMatchPossible:
                        return matchLevelRequired == MatchLevel.PGroup;
                    case Dataset.GGroupMatchPossible:
                        return matchLevelRequired == MatchLevel.GGroup;
                    case Dataset.FourFieldAllelesWithThreeFieldMatchPossible:
                        return matchLevelRequired == MatchLevel.FirstThreeFieldAllele && tgsTypingRequired == TgsHlaTypingCategory.FourFieldAllele;
                    case Dataset.ThreeFieldAllelesWithTwoFieldMatchPossible:
                        return matchLevelRequired == MatchLevel.FirstTwoFieldAllele && tgsTypingRequired == TgsHlaTypingCategory.ThreeFieldAllele;
                    case Dataset.AlleleStringOfSubtypesPossible:
                        return criteria.DatabaseDonorDetailsSets
                            .Any(d => d.MatchingTypingResolutions.DataAtPosition(l, p) == HlaTypingResolution.AlleleStringOfSubtypes);
                    case Dataset.NullAlleles:
                        // TODO: NOVA-1188: Allow matching on meta donors with null alleles when null matching implemented
                        return false;
                    case Dataset.AllelesWithNonNullExpressionSuffix:
                        return criteria.HasNonNullExpressionSuffix.DataAtPosition(l, p);
                    case Dataset.CDnaMatchPossible:
                        return matchLevelRequired == MatchLevel.CDna;
                    case Dataset.ProteinMatchPossible:
                        return matchLevelRequired == MatchLevel.Protein;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(dataset), dataset, null);
                }
            }).ToEnumerable().All(x => x);
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