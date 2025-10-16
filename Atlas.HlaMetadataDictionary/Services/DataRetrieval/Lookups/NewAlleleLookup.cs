using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.InternalModels.MetadataTableRows;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval.Lookups
{
    internal class NewAlleleLookup : HlaLookupBase
    {
        private const string newAllele = "NEW";

        public NewAlleleLookup(IHlaMetadataRepository hlaMetadataRepository) : base(hlaMetadataRepository)
        {
        }

        public override async Task<List<HlaMetadataTableRow>> PerformLookupAsync(Locus locus, string lookupName, string hlaNomenclatureVersion)
        {
            var row = new HlaMetadataTableRow
            {
                SerialisedHlaInfo = string.Empty,
                SerialisedHlaInfoType = string.Empty,
                LocusAsString = locus.ToString(),
                TypingMethodAsString = "Molecular",
                LookupName = newAllele
            };

            return new List<HlaMetadataTableRow> { row };
        }
    }
}