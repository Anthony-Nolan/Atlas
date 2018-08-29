using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Builders;
using Nova.SearchAlgorithm.Test.Validation.TestData.Exceptions;
using Nova.SearchAlgorithm.Test.Validation.TestData.Helpers;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;
using Nova.SearchAlgorithm.Test.Validation.TestData.Repositories;
using Nova.SearchAlgorithm.Test.Validation.TestData.Resources;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Services
{
    /// <summary>
    /// Generates Genotypes from the allele test data
    /// </summary>
    public static class GenotypeGenerator
    {
        private static readonly IAlleleRepository AlleleRepository = new AlleleRepository();
        private static readonly GenotypeCriteria DefaultCriteria = new GenotypeCriteriaBuilder().Build();


        public static Genotype GenerateGenotype(GenotypeCriteria criteria)
        {
            if (criteria == null)
            {
                return RandomGenotype(DefaultCriteria);
            }

            return RandomGenotype(criteria);
        }

        /// <summary>
        /// Creates a random full Genotype from the available TGS allele names
        /// </summary>
        private static Genotype RandomGenotype(GenotypeCriteria criteria)
        {
            var hla = new PhenotypeInfo<TgsAllele>();
            foreach (var locus in LocusHelpers.AllLoci())
            {
                var randomTgsAllele1 = RandomTgsAllele(locus, TypePositions.One, criteria);
                hla.SetAtLocus(locus, TypePositions.One, randomTgsAllele1);

                if (criteria.IsHomozygous.DataAtLocus(locus))
                {
                    hla.SetAtLocus(locus, TypePositions.Two, randomTgsAllele1);
                }
                else
                {
                    var randomTgsAllele2 = RandomTgsAllele(locus, TypePositions.Two, criteria);
                    hla.SetAtLocus(locus, TypePositions.Two, randomTgsAllele2);
                }
            }

            return new Genotype
            {
                Hla = hla
            };
        }

        private static TgsAllele RandomTgsAllele(Locus locus, TypePositions position, GenotypeCriteria criteria)
        {
            var dataset = criteria.AlleleSources.DataAtPosition(locus, position);

            var alleles = GetDataset(locus, position, dataset);
            var selectedAllele = alleles.GetRandomElement();

            var allelesForAlleleStringOfSubtypes = new List<AlleleTestData>();
            if (dataset == Dataset.AlleleStringOfSubtypesPossible)
            {
                var allelesValidForAlleleStringOfSubtypes = GetAllelesValidForAlleleStringOfSubtypes(alleles, selectedAllele);
                allelesForAlleleStringOfSubtypes = allelesValidForAlleleStringOfSubtypes.GetRandomSelection(1, 10).ToList();
            }

            return TgsAllele.FromTestDataAllele(
                selectedAllele,
                alleles.GetRandomSelection(1, 10),
                allelesForAlleleStringOfSubtypes
            );
        }

        /// <summary>
        /// Returns which test alleles from a list are valid for use in the allele string of subtypes
        /// The dataset selection will guarantee that such alleles must exist
        /// This method must select the alleles that
        /// (a) match the first field of the selected allele
        /// (b) do not match the second field of the selected allele (so we do not repeat subtypes in the string)
        /// </summary>
        private static List<AlleleTestData> GetAllelesValidForAlleleStringOfSubtypes(List<AlleleTestData> alleles, AlleleTestData selectedAllele)
        {
            var allelesWithCorrectFirstField = alleles
                .Where(a => AlleleSplitter.FirstField(a.AlleleName) == AlleleSplitter.FirstField(selectedAllele.AlleleName))
                .Where(a => AlleleSplitter.SecondField(a.AlleleName) != AlleleSplitter.SecondField(selectedAllele.AlleleName));

            return allelesWithCorrectFirstField
                .GroupBy(a => AlleleSplitter.FirstTwoFieldsAsString(a.AlleleName))
                .Select(gg => gg.Key)
                .Select(a => new AlleleTestData {AlleleName = a})
                .ToList();
        }

        private static List<AlleleTestData> GetDataset(Locus locus, TypePositions position, Dataset dataset)
        {
            switch (dataset)
            {
                case Dataset.FourFieldTgsAlleles:
                    return AlleleRepository.FourFieldAlleles().DataAtPosition(locus, position);
                case Dataset.ThreeFieldTgsAlleles:
                    return AlleleRepository.ThreeFieldAlleles().DataAtPosition(locus, position);
                case Dataset.TwoFieldTgsAlleles:
                    return AlleleRepository.TwoFieldAlleles().DataAtPosition(locus, position);
                case Dataset.TgsAlleles:
                    // Randomly choose dataset here rather than randomly choosing alleles from full dataset,
                    // as otherwise the data is skewed towards the larger dataset (4-field)
                    return new List<List<AlleleTestData>>
                    {
                        AlleleRepository.FourFieldAlleles().DataAtPosition(locus, position),
                        AlleleRepository.ThreeFieldAlleles().DataAtPosition(locus, position),
                        AlleleRepository.TwoFieldAlleles().DataAtPosition(locus, position)
                    }.GetRandomElement();
                case Dataset.PGroupMatchPossible:
                    return AlleleRepository.DonorAllelesForPGroupMatching().DataAtLocus(locus);
                case Dataset.GGroupMatchPossible:
                    return AlleleRepository.AllelesForGGroupMatching().DataAtPosition(locus, position);
                case Dataset.FourFieldAllelesWithThreeFieldMatchPossible:
                    return AlleleRepository.DonorAllelesWithThreeFieldMatchPossible().DataAtPosition(locus, position);
                case Dataset.ThreeFieldAllelesWithTwoFieldMatchPossible:
                    return AlleleRepository.AllelesWithTwoFieldMatchPossible().DataAtPosition(locus, position);
                case Dataset.AlleleStringOfSubtypesPossible:
                    return AlleleRepository.AllelesWithAlleleStringOfSubtypesPossible().DataAtPosition(locus, position);
                default:
                    throw new ArgumentOutOfRangeException(nameof(dataset), dataset, null);
            }
        }

        /// <summary>
        /// A Genotype for which all hla values do not match any others in the repository
        /// </summary>
        ///  TODO: NOVA-1590: Create more robust method of guaranteeing a mismatch
        /// As we're randomly selecting alleles for donors, there's a chance this will actually match
        public static readonly Genotype NonMatchingGenotype = new Genotype
        {
            Hla = NonMatchingAlleles.Alleles.Map((l, p, a) => TgsAllele.FromTestDataAllele(a))
        };
    }
}