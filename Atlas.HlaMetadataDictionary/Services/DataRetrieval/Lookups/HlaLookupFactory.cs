using System;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;
using Atlas.MultipleAlleleCodeDictionary;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval.Lookups
{
    internal static class HlaLookupFactory
    {
        public static HlaLookupBase GetLookupByHlaTypingCategory(
            HlaTypingCategory category,
            IHlaMetadataRepository hlaMetadataRepository,
            IAlleleNamesMetadataService alleleNamesMetadataService,
            IAlleleStringSplitterService alleleSplitter,
            INmdpCodeCache cache)
        {
            switch (category)
            {
                case HlaTypingCategory.Allele:
                    return new SingleAlleleLookup(
                        hlaMetadataRepository, 
                        alleleNamesMetadataService);
                case HlaTypingCategory.XxCode:
                    return new XxCodeLookup(
                        hlaMetadataRepository);
                case HlaTypingCategory.Serology:
                    return new SerologyLookup(
                        hlaMetadataRepository);                   
                case HlaTypingCategory.NmdpCode:
                    return new NmdpCodeLookup(
                        hlaMetadataRepository,
                        alleleNamesMetadataService,
                        cache);                    
                case HlaTypingCategory.AlleleStringOfNames:
                case HlaTypingCategory.AlleleStringOfSubtypes:
                    return new AlleleStringLookup(
                        hlaMetadataRepository, 
                        alleleNamesMetadataService, 
                        alleleSplitter);                    
                default:
                    throw new ArgumentException(
                        $"Dictionary lookup cannot be performed for HLA typing category: {category}.");
            }
        }
    }
}