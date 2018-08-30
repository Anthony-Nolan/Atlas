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
                hla.SetAtPosition(locus, TypePositions.One, randomTgsAllele1);

                if (criteria.IsHomozygous.DataAtLocus(locus))
                {
                    hla.SetAtPosition(locus, TypePositions.Two, randomTgsAllele1);
                }
                else
                {
                    var randomTgsAllele2 = RandomTgsAllele(locus, TypePositions.Two, criteria);
                    hla.SetAtPosition(locus, TypePositions.Two, randomTgsAllele2);
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

            var shouldContainDifferentAlleleGroups = criteria.AlleleStringContainsDifferentAntigenGroups.DataAtPosition(locus, position);

            return TgsAllele.FromTestDataAllele(
                selectedAllele,
                GetAllelesForAlleleStringOfNames(dataset, selectedAllele, alleles, shouldContainDifferentAlleleGroups),
                GetAllelesForAlleleStringOfSubtypes(dataset, selectedAllele, alleles)
            );
        }

        /// <summary>
        /// Selects a set of alleles to be used when generating an allele string of subtypes for the selected allele
        /// </summary>
        /// <param name="dataset">The selected dataset type. If not 'AlleleStringOfSubtypesPossible', no additional alleles can be selected</param>
        /// <param name="selectedAllele">The selected allele</param>
        /// <param name="alleles">The dataset of alleles the selected allele was chosen from</param>
        /// <returns></returns>
        private static IEnumerable<AlleleTestData> GetAllelesForAlleleStringOfSubtypes(
            Dataset dataset,
            AlleleTestData selectedAllele,
            IEnumerable<AlleleTestData> alleles
        )
        {
            var allelesForAlleleStringOfSubtypes = new List<AlleleTestData>();
            if (dataset == Dataset.AlleleStringOfSubtypesPossible)
            {
                var allelesValidForAlleleStringOfSubtypes = GetAllelesValidForAlleleStringOfSubtypes(alleles, selectedAllele);
                allelesForAlleleStringOfSubtypes = allelesValidForAlleleStringOfSubtypes.GetRandomSelection(1, 10).ToList();
            }

            return allelesForAlleleStringOfSubtypes;
        }

        /// <summary>
        /// By default, alleles sharing a first field with the selected allele are preferred, but not required
        /// Selects a set of alleles to be used when generating an allele string of names for the selected allele
        /// </summary>
        /// <param name="dataset">The selected dataset type.</param>
        /// <param name="selectedAllele">The selected allele</param>
        /// <param name="alleles">The dataset of alleles the selected allele was chosen from</param>
        /// <param name="shouldContainDifferentAlleleGroups">
        ///     When true, enforces that at least two first fields are represented among alleles in string
        /// </param>
        private static IEnumerable<AlleleTestData> GetAllelesForAlleleStringOfNames(
            Dataset dataset,
            AlleleTestData selectedAllele,
            IEnumerable<AlleleTestData> alleles,
            bool shouldContainDifferentAlleleGroups
        )
        {
            // This dataset does not have enough information to support building allele strings. 
            // This simple check may need to be extended at some point if:
            // (a) This is true for multiple datasets
            // (b) We want to build allele strings from > 1 dataset (e.g. add alleles with an expression suffix to a TGS allele string)
            if (dataset == Dataset.AllelesWithNonNullExpressionSuffix)
            {
                return new List<AlleleTestData>();
            }
            
            var selectedFirstField = AlleleSplitter.FirstField(selectedAllele.AlleleName);

            // Same allele should not appear twice in allele string
            var nonMatchingAlleles = alleles.Where(a => a.AlleleName != selectedAllele.AlleleName).ToList();

            var isUniqueFirstField = nonMatchingAlleles.All(a => AlleleSplitter.FirstField(a.AlleleName) != selectedFirstField);

            var validAlleles = nonMatchingAlleles.Where(a =>
            {
                // Allow any alleles to be selected if:
                // (a) No other alleles share a first field with the selected allele
                // (we could enforce this at the time of data selection, but this isn't currently necessary, and feels like a duplication of the allele string of subtypes logic)
                // (b) The allele string should explicitly contain multiple first fields
                if (isUniqueFirstField || shouldContainDifferentAlleleGroups)
                {
                    return true;
                }

                return AlleleSplitter.FirstField(a.AlleleName) == selectedFirstField;
            }).ToList();

            var allelesForString = validAlleles.GetRandomSelection(1, 10).ToList();

            // If random selection has only picked alleles with the same first field, ensure an allele with a different first field is used
            if (shouldContainDifferentAlleleGroups && allelesForString.All(a => AlleleSplitter.FirstField(a.AlleleName) == selectedFirstField))
            {
                var alleleWithSharedFirstField = validAlleles.FirstOrDefault(a => AlleleSplitter.FirstField(a.AlleleName) != selectedFirstField);
                if (alleleWithSharedFirstField == null)
                {
                    throw new InvalidTestDataException(
                        $"No other alleles sharing a first field were found. Selected allele: {selectedAllele.AlleleName}");
                }

                allelesForString.Add(alleleWithSharedFirstField);
            }

            return allelesForString;
        }

        /// <summary>
        /// Returns which test alleles from a list are valid for use in the allele string of subtypes
        /// The dataset selection will guarantee that such alleles must exist
        /// This method must select the alleles that
        /// (a) match the first field of the selected allele
        /// (b) do not match the second field of the selected allele (so we do not repeat subtypes in the string)
        /// </summary>
        private static List<AlleleTestData> GetAllelesValidForAlleleStringOfSubtypes(
            IEnumerable<AlleleTestData> alleles,
            AlleleTestData selectedAllele
        )
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
                case Dataset.NullAlleles:
                    return AlleleRepository.NullAlleles().DataAtPosition(locus, position);
                case Dataset.AllelesWithNonNullExpressionSuffix:
                    return AlleleRepository.AllelesWithNonNullExpressionSuffix().DataAtPosition(locus, position);
                case Dataset.CDnaMatchPossible:
                    return AlleleRepository.AllelesForCDnaMatching().DataAtLocus(locus);
                case Dataset.ProteinMatchPossible:
                    return AlleleRepository.AllelesForProteinMatching().DataAtPosition(locus, position);
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
            Hla = NonMatchingAlleles.Alleles.Map((l, a) => TgsAllele.FromTestDataAllele(a)).ToPhenotypeInfo((l, a) => a),
        };
    }
}