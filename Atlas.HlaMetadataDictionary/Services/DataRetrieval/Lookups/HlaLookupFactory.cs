using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;
using System;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval.Lookups
{
    internal static class HlaLookupFactory
    {
        public static HlaLookupBase GetLookupByHlaTypingCategory(
            HlaTypingCategory category,
            IHlaMetadataRepository hlaMetadataRepository,
            IAlleleNamesMetadataService alleleNamesMetadataService,
            IAlleleNamesExtractor alleleNamesExtractor,
            IMacDictionary macDictionary,
            IAlleleGroupExpander alleleGroupExpander)
        {
            return category switch
            {
                HlaTypingCategory.Allele => new SingleAlleleLookup(hlaMetadataRepository, alleleNamesMetadataService),
                HlaTypingCategory.XxCode => new XxCodeLookup(hlaMetadataRepository),
                HlaTypingCategory.Serology => new SerologyLookup(hlaMetadataRepository),
                HlaTypingCategory.NmdpCode => new MacLookup(hlaMetadataRepository, alleleNamesMetadataService, macDictionary),
                HlaTypingCategory.AlleleStringOfNames => new AlleleStringLookup(hlaMetadataRepository, alleleNamesMetadataService, alleleNamesExtractor),
                HlaTypingCategory.AlleleStringOfSubtypes => new AlleleStringLookup(hlaMetadataRepository, alleleNamesMetadataService, alleleNamesExtractor),
                HlaTypingCategory.PGroup => new AlleleGroupLookup(hlaMetadataRepository, alleleNamesMetadataService, alleleGroupExpander),
                HlaTypingCategory.GGroup => new AlleleGroupLookup(hlaMetadataRepository, alleleNamesMetadataService, alleleGroupExpander),
                _ => throw new ArgumentException(
                    $"Dictionary lookup cannot be performed for HLA typing category: {category}.")
            };
        }
    }
}