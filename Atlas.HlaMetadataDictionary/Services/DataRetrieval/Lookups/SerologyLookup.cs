using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.HlaMetadataDictionary.InternalModels.MetadataTableRows;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval.Lookups
{
    internal class SerologyLookup : HlaLookupBase
    {
        public SerologyLookup(IHlaMetadataRepository hlaMetadataRepository) : base(hlaMetadataRepository)
        {
        }

        public override async Task<IEnumerable<HlaMetadataTableRow>> PerformLookupAsync(Locus locus, string lookupName, string hlaNomenclatureVersion)
        {
            var row = await GetHlaMetadataRowIfExists(locus, lookupName, TypingMethod.Serology, hlaNomenclatureVersion);
            return new List<HlaMetadataTableRow> { row };
        }
    }
}
