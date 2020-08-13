using System;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Helpers;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Exceptions;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.Hla;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.PatientDataSelection;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Repositories;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Resources.Alleles;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Services.PatientDataSelection.DataSelectors
{
    /// <summary>
    /// Responsible for deciding a patient's hla based on criteria
    /// </summary>
    public interface IPatientHlaSelector
    {
        PhenotypeInfo<string> GetPatientHla(MetaDonor metaDonor, PatientHlaSelectionCriteria criteria);
    }

    public class PatientHlaSelector : IPatientHlaSelector
    {
        private readonly IAlleleRepository alleleRepository;

        /// <summary>
        /// A Genotype for which all hla values do not match any others in the repository
        /// </summary>
        private static readonly Genotype NonMatchingGenotype = new Genotype
        {
            Hla = NonMatchingAlleles.NonMatchingPatientAlleles.Map((l, a) => TgsAllele.FromTestDataAllele(a)).ToPhenotypeInfo((l, a) => a),
        };

        /// <summary>
        /// A Genotype for which all hla values are null alleles, and are not the same as any other null alleles in the repository
        /// </summary>
        private static readonly Genotype NonMatchingNullAlleleGenotype = new Genotype
        {
            Hla = NonMatchingAlleles.NonMatchingNullAlleles.Map((l, a) => TgsAllele.FromTestDataAllele(a)).ToPhenotypeInfo((l, a) => a),
        };

        public PatientHlaSelector(IAlleleRepository alleleRepository)
        {
            this.alleleRepository = alleleRepository;
        }

        public PhenotypeInfo<string> GetPatientHla(MetaDonor metaDonor, PatientHlaSelectionCriteria criteria)
        {
            return metaDonor.Genotype.Hla.MapByLocus((locus, hla) => GetHlaName(locus, hla.Position1, hla.Position2, criteria));
        }

        private LocusInfo<string> GetHlaName(Locus locus, TgsAllele tgsAllele1, TgsAllele tgsAllele2, PatientHlaSelectionCriteria criteria)
        {
            TgsAllele allele1;
            TgsAllele allele2;

            if (criteria.IsHomozygous.GetLocus(locus))
            {
                var allele = GetHomozygousAllele(locus, tgsAllele1, tgsAllele2, criteria);
                allele1 = allele;
                allele2 = allele;
            }
            else
            {
                var orientation = GetDesiredMatchOrientation(locus, criteria);

                switch (orientation)
                {
                    case MatchOrientation.Direct:
                        allele1 = GetTgsAllele(locus, LocusPosition.One, tgsAllele1, tgsAllele2, criteria);
                        allele2 = GetTgsAllele(locus, LocusPosition.Two, tgsAllele2, tgsAllele1, criteria);
                        break;
                    case MatchOrientation.Cross:
                        allele1 = GetTgsAllele(locus, LocusPosition.One, tgsAllele2, tgsAllele1, criteria);
                        allele2 = GetTgsAllele(locus, LocusPosition.Two, tgsAllele1, tgsAllele2, criteria);
                        break;
                    case MatchOrientation.Arbitrary:
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            var typingResolution1 = criteria.PatientTypingResolutions.GetPosition(locus, LocusPosition.One);
            var typingResolution2 = criteria.PatientTypingResolutions.GetPosition(locus, LocusPosition.Two);

            var hla1 = allele1.GetHlaForResolution(typingResolution1);
            var hla2 = allele2.GetHlaForResolution(typingResolution2);
            return new LocusInfo<string>(hla1, hla2);
        }

        private static MatchOrientation GetDesiredMatchOrientation(Locus locus, PatientHlaSelectionCriteria criteria)
        {
            var orientation = criteria.Orientations.GetLocus(locus);

            if (orientation == MatchOrientation.Arbitrary)
            {
                // Some match level specific test data is curated independently for each position, with direct matches in mind.
                // If cross matches were attempted, we may end up with a better match grade than desired, or without possible patient data
                var directOnlyMatchLevels = new[] {MatchLevel.GGroup, MatchLevel.FirstThreeFieldAllele, MatchLevel.FirstTwoFieldAllele};
                var matchLevels = criteria.MatchLevels.GetLocus(locus);

                var hlaSourceAtLocus = criteria.HlaSources.GetLocus(locus);
                // Null mismatches are specified at a specific locus - if one is specified, and a cross orientation chosen,
                // we can end up with two null alleles selected (which causes a mismatch result, where a single null allele would be a match)
                var isMismatchedNullAlleleAtLocus = hlaSourceAtLocus.Position1 == PatientHlaSource.NullAlleleMismatch ||
                                                    hlaSourceAtLocus.Position2 == PatientHlaSource.NullAlleleMismatch;

                var shouldForceDirectOrientation = new[] {matchLevels.Position1, matchLevels.Position2}.Intersect(directOnlyMatchLevels).Any() ||
                                                   isMismatchedNullAlleleAtLocus;
                return shouldForceDirectOrientation
                    ? MatchOrientation.Direct
                    : new[] {MatchOrientation.Cross, MatchOrientation.Direct}.GetRandomElement();
            }

            return orientation;
        }

        private static TgsAllele GetHomozygousAllele(Locus locus, TgsAllele tgsAllele1, TgsAllele tgsAllele2, PatientHlaSelectionCriteria criteria)
        {
            var shouldMatchAtLocus = criteria.HlaMatches.GetLocus(locus);
            if (shouldMatchAtLocus.Position1 ^ shouldMatchAtLocus.Position2)
            {
                return shouldMatchAtLocus.Position1 ? tgsAllele1 : tgsAllele2;
            }

            if (!shouldMatchAtLocus.Position1 && !shouldMatchAtLocus.Position2)
            {
                return tgsAllele1;
            }

            if (tgsAllele1 != tgsAllele2)
            {
                throw new HlaSelectionException("Cannot selected 2/2 match for homozygous patient when donor is not also homozygous");
            }

            return tgsAllele1;
        }

        private TgsAllele GetTgsAllele(
            Locus locus,
            LocusPosition position,
            TgsAllele genotypeAllele,
            TgsAllele otherGenotypeAllele,
            PatientHlaSelectionCriteria criteria
        )
        {
            switch (criteria.HlaSources.GetPosition(locus, position))
            {
                case PatientHlaSource.ExpressingAlleleMismatch:
                    return GetNonMatchingAllele(locus, position);
                case PatientHlaSource.NullAlleleMismatch:
                    return GetNonMatchingNullAllele(locus, position);
                case PatientHlaSource.Match:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            switch (criteria.MatchLevels.GetPosition(locus, position))
            {
                case MatchLevel.PGroup:
                    return GetPGroupMatchLevelTgsAllele(locus);
                case MatchLevel.GGroup:
                    return GetGGroupMatchLevelTgsAllele(locus, position, genotypeAllele);
                case MatchLevel.FirstThreeFieldAllele:
                    return GetThreeFieldMatchingTgsAllele(locus, position, genotypeAllele, otherGenotypeAllele);
                case MatchLevel.CDna:
                case MatchLevel.Allele:
                    return genotypeAllele;
                case MatchLevel.Protein:
                case MatchLevel.FirstTwoFieldAllele:
                    return GetTwoFieldMatchingTgsAllele(locus, position, genotypeAllele, otherGenotypeAllele);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static TgsAllele GetNonMatchingAllele(Locus locus, LocusPosition position)
        {
            return NonMatchingGenotype.Hla.GetPosition(locus, position);
        }

        private static TgsAllele GetNonMatchingNullAllele(Locus locus, LocusPosition position)
        {
            return NonMatchingNullAlleleGenotype.Hla.GetPosition(locus, position);
        }

        private TgsAllele GetPGroupMatchLevelTgsAllele(Locus locus)
        {
            var alleleAtLocus = alleleRepository.PatientAllelesForPGroupMatching().GetLocus(locus);

            return TgsAllele.FromTestDataAllele(alleleAtLocus);
        }

        private TgsAllele GetGGroupMatchLevelTgsAllele(Locus locus, LocusPosition position, TgsAllele genotypeAllele)
        {
            var allelesAtLocus = alleleRepository.AllelesForGGroupMatching().GetPosition(locus, position);
            var allele = allelesAtLocus.First(a => a.AlleleName != genotypeAllele.TgsTypedAllele);

            return TgsAllele.FromTestDataAllele(allele);
        }

        private TgsAllele GetThreeFieldMatchingTgsAllele(
            Locus locus,
            LocusPosition position,
            TgsAllele genotypeAllele,
            TgsAllele otherGenotypeAllele
        )
        {
            var alleles = alleleRepository.PatientAllelesWithThreeFieldMatchPossible().GetPosition(locus, position);

            // alleles that match the first three fields
            var matchingAlleles = alleles.Where(a =>
            {
                var donorAlleleThreeFields = AlleleSplitter.FirstThreeFields(genotypeAllele.TgsTypedAllele);
                var alleleFirstThreeFields = AlleleSplitter.FirstThreeFields(a.AlleleName);
                return donorAlleleThreeFields.SequenceEqual(alleleFirstThreeFields);
            });

            // alleles that match the first three field, but not the fourth
            var validAlleles = matchingAlleles
                // Ensure that the allele is not an exact allele direct match
                .Where(a => a.AlleleName != genotypeAllele.TgsTypedAllele)
                // Ensure that the allele is not an exact allele cross match
                .Where(a => a.AlleleName != otherGenotypeAllele.TgsTypedAllele)
                .ToList();

            if (validAlleles.Count == 0)
            {
                throw new InvalidTestDataException(
                    $"No valid patient alleles found for the following donor data: {genotypeAllele.TgsTypedAllele} & {otherGenotypeAllele.TgsTypedAllele} at locus {locus}");
            }

            var selectedAllele = validAlleles.GetRandomElement();
            return TgsAllele.FromTestDataAllele(selectedAllele);
        }

        private TgsAllele GetTwoFieldMatchingTgsAllele(
            Locus locus,
            LocusPosition position,
            TgsAllele genotypeAllele,
            TgsAllele otherGenotypeAllele
        )
        {
            var alleles = alleleRepository.AllelesWithTwoFieldMatchPossible().GetPosition(locus, position);

            // alleles that match the first two fields
            var matchingAlleles = alleles.Where(a =>
            {
                var donorAlleleTwoFields = AlleleSplitter.FirstTwoFields(genotypeAllele.TgsTypedAllele);
                var alleleFirstTwoFields = AlleleSplitter.FirstTwoFields(a.AlleleName);
                return donorAlleleTwoFields.SequenceEqual(alleleFirstTwoFields);
            });

            var validAlleles = matchingAlleles
                // Ensure that the allele is not an exact allele direct match
                .Where(a => a.AlleleName != genotypeAllele.TgsTypedAllele)
                // Ensure that the allele is not an exact allele cross match
                .Where(a => a.AlleleName != otherGenotypeAllele.TgsTypedAllele)
                .ToList();

            if (validAlleles.Count == 0)
            {
                throw new InvalidTestDataException(
                    $"No valid patient alleles found for the following donor data: {genotypeAllele.TgsTypedAllele} & {otherGenotypeAllele.TgsTypedAllele} at locus {locus}");
            }

            var selectedAllele = validAlleles.GetRandomElement();
            return TgsAllele.FromTestDataAllele(selectedAllele);
        }
    }
}