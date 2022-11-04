using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;
using Atlas.HlaMetadataDictionary.Repositories;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration.AlleleNames
{
    internal interface IAlleleNameVariantsExtractor
    {
        /// <summary>
        /// Get name variants that could be submitted in a HLA lookup but have never been assigned to a DNA sequence.
        /// E.g., in decoded MAC strings, only the first two fields are used, and expression letters are removed from allele names.
        /// If the allele is not deleted, variants will be generated from the most recently assigned name within the referenced HLA nomenclature version.
        /// Else, for deleted alleles, variants will be generated from the lookup name itself.
        /// </summary>
        IEnumerable<IAlleleNameMetadata> GetAlleleNames(IEnumerable<IAlleleNameMetadata> originalAlleleNames, string hlaNomenclatureVersion);
    }

    internal class AlleleNameVariantsExtractor : AlleleNamesExtractorBase, IAlleleNameVariantsExtractor
    {
        public AlleleNameVariantsExtractor(IWmdaDataRepository dataRepository)
            : base(dataRepository)
        {
        }

        public IEnumerable<IAlleleNameMetadata> GetAlleleNames(IEnumerable<IAlleleNameMetadata> originalAlleleNames, string hlaNomenclatureVersion)
        {
            var variantsNotFoundInHistories = originalAlleleNames.SelectMany(n => GetAlleleNameVariantsNotFoundInHistories(n, hlaNomenclatureVersion)).ToList();
            return GroupAlleleNamesByLocusAndLookupName(variantsNotFoundInHistories);
        }

        private IEnumerable<IAlleleNameMetadata> GetAlleleNameVariantsNotFoundInHistories(IAlleleNameMetadata alleleName, string hlaNomenclatureVersion)
        {
            var currentName = IsLookupNameMarkedAsDeletedInHlaNom(alleleName, hlaNomenclatureVersion)
                ? alleleName.LookupName
                : alleleName.CurrentAlleleNames.First();

            var typingFromCurrentName = new AlleleTyping(alleleName.Locus, currentName);

            return typingFromCurrentName
                .NameVariantsTruncatedByFieldAndOrExpressionSuffix
                .Where(nameVariant => AlleleNameIsNotInHistories(typingFromCurrentName.TypingLocus, nameVariant, hlaNomenclatureVersion))
                .Select(nameVariant => new AlleleNameMetadata(
                    alleleName.Locus,
                    nameVariant,
                    alleleName.CurrentAlleleNames));
        }

        private bool IsLookupNameMarkedAsDeletedInHlaNom(IHlaMetadata allele, string hlaNomenclatureVersion)
        {
            var typingFromLookupName = new AlleleTyping(allele.Locus, allele.LookupName);

            return AllelesInVersionOfHlaNom(hlaNomenclatureVersion)
                .SingleOrDefault(a => a.TypingEquals(typingFromLookupName))
                ?.IsDeleted ?? false;
        }

        private static IEnumerable<IAlleleNameMetadata> GroupAlleleNamesByLocusAndLookupName(IEnumerable<IAlleleNameMetadata> alleleNameVariants)
        {
            var groupedEntries = alleleNameVariants
                .GroupBy(e => new { e.Locus, e.LookupName })
                .Select(e => new AlleleNameMetadata(
                    e.Key.Locus,
                    e.Key.LookupName,
                    e.SelectMany(x => x.CurrentAlleleNames).Distinct()
                ));

            return groupedEntries;
        }
    }
}
