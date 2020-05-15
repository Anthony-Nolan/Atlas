using Atlas.Utils.Hla.Services;
using Atlas.HlaMetadataDictionary.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Utils.Models;

namespace Atlas.HlaMetadataDictionary.Services.Lookups
{
    internal class AlleleStringLookup : AlleleNamesLookupBase
    {
        private readonly IAlleleStringSplitterService alleleSplitter;
        
        public AlleleStringLookup(
            IHlaLookupRepository hlaLookupRepository,
            IAlleleNamesLookupService alleleNamesLookupService,
            IAlleleStringSplitterService alleleSplitter)
            : base(hlaLookupRepository, alleleNamesLookupService)
        {
            this.alleleSplitter = alleleSplitter;
        }

        protected override async Task<IEnumerable<string>> GetAlleleLookupNames(Locus locus, string lookupName)
        {
            return await Task.Run(() => alleleSplitter.GetAlleleNamesFromAlleleString(lookupName));
        }
    }
}
