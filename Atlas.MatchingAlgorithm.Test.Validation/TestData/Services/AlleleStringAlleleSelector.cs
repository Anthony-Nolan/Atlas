using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.GeneticData.Hla.Services.AlleleNameUtils;
using Atlas.Common.Maths;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Exceptions;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.Hla;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Services
{
    public static class AlleleStringAlleleSelector
    {
        /// <summary>
        /// Selects a set of alleles to be used when generating an allele string of subtypes for the selected allele
        /// </summary>
        /// <param name="dataset">The selected dataset type. If not 'AlleleStringOfSubtypesPossible', no additional alleles can be selected</param>
        /// <param name="selectedAllele">The selected allele</param>
        /// <param name="alleles">The dataset of alleles the selected allele was chosen from</param>
        /// <returns></returns>
        public static List<AlleleTestData> GetAllelesForAlleleStringOfSubtypes(
            Dataset dataset,
            AlleleTestData selectedAllele,
            IEnumerable<AlleleTestData> alleles
        )
        {
            var allelesForAlleleStringOfSubtypes = new List<AlleleTestData>();
            if (dataset == Dataset.AlleleStringOfSubtypesPossible)
            {
                var allelesValidForAlleleStringOfSubtypes = GetAllelesValidForAlleleStringOfSubtypes(alleles, selectedAllele);
                if (allelesValidForAlleleStringOfSubtypes.IsNullOrEmpty())
                {
                    throw new InvalidTestDataException("Allele string of subtypes required, but no valid alleles to use in the string exist");
                }

                allelesForAlleleStringOfSubtypes = allelesValidForAlleleStringOfSubtypes.GetRandomSelection(1, 10).ToList();
            }

            return allelesForAlleleStringOfSubtypes;
        }

        public static List<AlleleTestData> GetAllelesForAlleleStringOfNamesWithSinglePGroup(
            AlleleTestData selectedAllele,
            IEnumerable<AlleleTestData> alleles
        )
        {
            return GetAllelesForAlleleStringOfNamesByPGroup(
                selectedAllele,
                alleles,
                a => a.PGroup == selectedAllele.PGroup && a.AlleleName != selectedAllele.AlleleName);
        }

        public static List<AlleleTestData> GetAllelesForAlleleStringOfNamesWithMultiplePGroups(
            AlleleTestData selectedAllele,
            IEnumerable<AlleleTestData> alleles
        )
        {
            return GetAllelesForAlleleStringOfNamesByPGroup(
                selectedAllele,
                alleles,
                a => a.PGroup != selectedAllele.PGroup);
        }

        public static List<AlleleTestData> GetAllelesForAlleleStringOfNamesByPGroup(
            AlleleTestData selectedAllele,
            IEnumerable<AlleleTestData> alleles,
            Func<AlleleTestData, bool> pGroupFilterFunc
        )
        {
            // If we do not know the p-group for the selected allele, this string cannot be generated
            if (selectedAllele.PGroup == null)
            {
                return new List<AlleleTestData>();
            }

            var filteredAlleles = alleles
                .Where(pGroupFilterFunc)
                .ToList();

            // If no alleles found, string cannot be generated
            return filteredAlleles.IsNullOrEmpty()
                ? new List<AlleleTestData>()
                : filteredAlleles.GetRandomSelection(1, 10).ToList();
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
        public static List<AlleleTestData> GetAllelesForAlleleStringOfNames(
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

            if (validAlleles.IsNullOrEmpty())
            {
                throw new InvalidTestDataException($"No alleles valid for use in an allele string (of names) found in dataset: {dataset}");
            }

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
                .Select(a => new AlleleTestData { AlleleName = a })
                .ToList();
        }
    }
}