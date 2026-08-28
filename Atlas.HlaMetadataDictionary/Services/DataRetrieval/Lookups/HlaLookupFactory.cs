using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.InternalExceptions;
using Atlas.HlaMetadataDictionary.InternalModels.MetadataTableRows;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval.Lookups
{
    internal static class HlaLookupFactory
    {
        public static HlaLookupBase GetLookupByHlaTypingCategory(
            Locus locus,
            string lookupName,
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
                HlaTypingCategory.SmallGGroup => new AlleleGroupLookup(hlaMetadataRepository, alleleNamesMetadataService, alleleGroupExpander),
                HlaTypingCategory.NEW => new NewAlleleLookup(hlaMetadataRepository), 
                // A category this dictionary cannot look up is a name with no data, not an infrastructure fault. As an
                // ArgumentException it only reached callers as a missing name because GetMetadata re-labelled it.
                _ => throw new InvalidHlaException(locus, lookupName)
            };
        }
    }
}