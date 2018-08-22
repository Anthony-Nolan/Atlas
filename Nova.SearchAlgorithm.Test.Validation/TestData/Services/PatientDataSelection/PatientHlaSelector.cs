using System;
using System.Linq;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Exceptions;
using Nova.SearchAlgorithm.Test.Validation.TestData.Helpers;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.PatientDataSelection;
using Nova.SearchAlgorithm.Test.Validation.TestData.Repositories;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection
{
    public interface IPatientHlaSelector
    {
        PhenotypeInfo<string> GetPatientHla(MetaDonor metaDonor, PatientHlaSelectionCriteria criteria);
    }

    public class PatientHlaSelector : IPatientHlaSelector
    {
        private readonly IAlleleRepository alleleRepository;

        public PatientHlaSelector(IAlleleRepository alleleRepository)
        {
            this.alleleRepository = alleleRepository;
        }

        public PhenotypeInfo<string> GetPatientHla(MetaDonor metaDonor, PatientHlaSelectionCriteria criteria)
        {
            return metaDonor.Genotype.Hla.MapByLocus((locus, allele1, allele2) => GetHlaName(locus, allele1, allele2, criteria));
        }

        private Tuple<string, string> GetHlaName(Locus locus, TgsAllele tgsAllele1, TgsAllele tgsAllele2, PatientHlaSelectionCriteria criteria)
        {
            TgsAllele allele1;
            TgsAllele allele2;

            if (criteria.IsHomozygous.DataAtLocus(locus))
            {
                var allele = GetHomozygousAllele(locus, tgsAllele1, tgsAllele2, criteria);
                allele1 = allele;
                allele2 = allele;
            }
            else
            {
                allele1 = GetTgsAllele(locus, TypePositions.One, tgsAllele1, criteria);
                allele2 = GetTgsAllele(locus, TypePositions.Two, tgsAllele2, criteria);
            }

            var typingResolution1 = criteria.PatientTypingResolutions.DataAtPosition(locus, TypePositions.One);
            var typingResolution2 = criteria.PatientTypingResolutions.DataAtPosition(locus, TypePositions.Two);

            var hla1 = allele1.GetHlaForCategory(typingResolution1);
            var hla2 = allele2.GetHlaForCategory(typingResolution2);
            return new Tuple<string, string>(hla1, hla2);
        }

        private static TgsAllele GetHomozygousAllele(Locus locus, TgsAllele tgsAllele1, TgsAllele tgsAllele2, PatientHlaSelectionCriteria criteria)
        {
            var shouldMatchAtLocus = criteria.HlaMatches.DataAtLocus(locus);
            if (shouldMatchAtLocus.Item1 ^ shouldMatchAtLocus.Item2)
            {
                return shouldMatchAtLocus.Item1 ? tgsAllele1 : tgsAllele2;
            }

            if (!shouldMatchAtLocus.Item1 && !shouldMatchAtLocus.Item2)
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
            TypePositions position,
            TgsAllele genotypeAllele,
            PatientHlaSelectionCriteria criteria
        )
        {
            // patient should be mismatched at this position
            if (!criteria.HlaMatches.DataAtPosition(locus, position))
            {
                return GetNonMatchingAllele(locus, position);
            }

            switch (criteria.MatchLevels.DataAtPosition(locus, position))
            {
                case MatchLevel.PGroup:
                    return GetPGroupMatchLevelTgsAllele(locus);
                case MatchLevel.GGroup:
                    return GetGGroupMatchLevelTgsAllele(locus, position, genotypeAllele);
                case MatchLevel.ThreeFieldAllele:
                    return GetThreeFieldMatchingTgsAllele(locus, position, genotypeAllele);
                case MatchLevel.Allele:
                    return genotypeAllele;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // TODO: NOVA-1654: Remove static dependency on GenotypeGenerator so we can unit test this
        private static TgsAllele GetNonMatchingAllele(Locus locus, TypePositions position)
        {
            return GenotypeGenerator.NonMatchingGenotype.Hla.DataAtPosition(locus, position);
        }

        private TgsAllele GetPGroupMatchLevelTgsAllele(Locus locus)
        {
            var alleleAtLocus = alleleRepository.PatientAllelesForPGroupMatching().DataAtLocus(locus);

            return TgsAllele.FromTestDataAllele(alleleAtLocus, locus);
        }

        private TgsAllele GetGGroupMatchLevelTgsAllele(Locus locus, TypePositions position, TgsAllele genotypeAllele)
        {
            var allelesAtLocus = alleleRepository.AllelesForGGroupMatching().DataAtPosition(locus, position);
            var allele = allelesAtLocus.First(a => a.AlleleName != genotypeAllele.TgsTypedAllele);

            return TgsAllele.FromTestDataAllele(allele, locus);
        }

        private TgsAllele GetThreeFieldMatchingTgsAllele(Locus locus, TypePositions position, TgsAllele genotypeAllele)
        {
            var alleles = alleleRepository.AllelesWithThreeFieldMatchPossible().DataAtPosition(locus, position);
            var matchingAlleles = alleles.Where(a =>
            {
                var donorAlleleThreeFields = AlleleSplitter.FirstThreeFields(genotypeAllele.TgsTypedAllele);
                var alleleFirstThreeFields = AlleleSplitter.FirstThreeFields(a.AlleleName);
                return donorAlleleThreeFields.SequenceEqual(alleleFirstThreeFields);
            });
            var selectedAllele = matchingAlleles.Where(a => a.AlleleName != genotypeAllele.TgsTypedAllele).ToList().GetRandomElement();
            return TgsAllele.FromTestDataAllele(selectedAllele, locus);
        }
    }
}