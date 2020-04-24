using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Exceptions;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.Hla;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.PatientDataSelection;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Repositories;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Services.PatientDataSelection
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
            return FulfilsDonorTypeCriteria(criteria, metaDonor);
        }

        private static bool FulfilsDonorHlaCriteria(MetaDonorSelectionCriteria criteria, MetaDonor metaDonor)
        {
            return FulfilsHomozygousCriteria(criteria, metaDonor)
                   && FulfilsExpressionSuffixCriteria(criteria, metaDonor)
                   && FulfilsNullAlleleCriteria(criteria, metaDonor)
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

        private static bool FulfilsNullAlleleCriteria(MetaDonorSelectionCriteria criteria, MetaDonor metaDonor)
        {
            var perLocusFulfilment = criteria.IsNullExpressing.Map((locus, position, shouldBeNullExpressing) =>
                !shouldBeNullExpressing
                || metaDonor.GenotypeCriteria.AlleleSources.DataAtPosition(locus, position) == Dataset.NullAlleles);

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
                var matchLevel = criteria.MatchLevels.DataAtPosition(l, p);

                switch (dataset)
                {
                    case Dataset.PGroupMatchPossible:
                        return matchLevel == MatchLevel.PGroup;
                    case Dataset.GGroupMatchPossible:
                        return matchLevel == MatchLevel.GGroup;
                    case Dataset.CDnaMatchPossible:
                        return matchLevel == MatchLevel.CDna;
                    case Dataset.ProteinMatchPossible:
                        return matchLevel == MatchLevel.Protein;
                    case Dataset.FourFieldAllelesWithThreeFieldMatchPossible:
                        return matchLevel == MatchLevel.FirstThreeFieldAllele
                               && tgsTypingRequired == TgsHlaTypingCategory.FourFieldAllele;
                    case Dataset.ThreeFieldAllelesWithTwoFieldMatchPossible:
                        return matchLevel == MatchLevel.FirstTwoFieldAllele
                               && tgsTypingRequired == TgsHlaTypingCategory.ThreeFieldAllele;
                    case Dataset.FourFieldTgsAlleles:
                        return matchLevel == MatchLevel.Allele
                               && tgsTypingRequired == TgsHlaTypingCategory.FourFieldAllele;
                    case Dataset.ThreeFieldTgsAlleles:
                        return matchLevel == MatchLevel.Allele
                               && tgsTypingRequired == TgsHlaTypingCategory.ThreeFieldAllele;
                    case Dataset.TwoFieldTgsAlleles:
                        return matchLevel == MatchLevel.Allele
                               && tgsTypingRequired == TgsHlaTypingCategory.TwoFieldAllele;
                    case Dataset.TgsAlleles:
                        return matchLevel == MatchLevel.Allele
                               && tgsTypingRequired == TgsHlaTypingCategory.Arbitrary;
                    case Dataset.AlleleStringOfSubtypesPossible:
                        return criteria.DatabaseDonorDetailsSets
                            .Any(d => d.MatchingTypingResolutions.DataAtPosition(l, p) == HlaTypingResolution.AlleleStringOfSubtypes);
                    case Dataset.NullAlleles:
                        return criteria.IsNullExpressing.DataAtPosition(l, p);
                    case Dataset.AllelesWithNonNullExpressionSuffix:
                        return criteria.HasNonNullExpressionSuffix.DataAtPosition(l, p);
                    case Dataset.AllelesWithStringsOfSingleAndMultiplePGroupsPossible:
                        var resolutions = new List<HlaTypingResolution>
                        {
                            HlaTypingResolution.Unambiguous,
                            HlaTypingResolution.AlleleStringOfNamesWithMultiplePGroups,
                            HlaTypingResolution.AlleleStringOfNamesWithSinglePGroup
                        };
                        return criteria.DatabaseDonorDetailsSets.Any(d => resolutions.Contains(d.MatchingTypingResolutions.DataAtPosition(l, p)));
                    default:
                        throw new ArgumentOutOfRangeException(nameof(dataset), dataset, null);
                }
            }).ToEnumerable().All(x => x);
        }

        private static bool FulfilsDonorTypeCriteria(MetaDonorSelectionCriteria criteria, MetaDonor metaDonor)
        {
            return criteria.MatchingDonorType == metaDonor.DonorType;
        }
    }
}