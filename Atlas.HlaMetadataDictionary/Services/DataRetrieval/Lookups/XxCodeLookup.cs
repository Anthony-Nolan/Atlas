using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.Models.LookupEntities;
using Atlas.HlaMetadataDictionary.Repositories.LookupRepositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval.Lookups
{
    internal class XxCodeLookup : HlaLookupBase
    {
        public XxCodeLookup(IHlaMetadataRepository hlaMetadataRepository) : 
            base(hlaMetadataRepository)
        {
        }

        public override async Task<IEnumerable<HlaMetadataTableRow>> PerformLookupAsync(Locus locus, string lookupName, string hlaNomenclatureVersion)
        {
            var row = await GetHlaMetadataRowIfExists(locus, lookupName, TypingMethod.Molecular, hlaNomenclatureVersion);
            return new List<HlaMetadataTableRow> { row };
        }
    }
}
