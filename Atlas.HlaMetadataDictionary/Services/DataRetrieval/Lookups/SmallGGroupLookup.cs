using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval.Lookups
{
    internal class SmallGGroupLookup : AlleleNamesLookupBase
    {
        private readonly IAlleleGroupExpander alleleGroupExpander;
        private readonly ISmallGGroupToPGroupMetadataService smallGGroupToPGroupMetadataService;

        public SmallGGroupLookup(
            IHlaMetadataRepository hlaMetadataRepository,
            IAlleleNamesMetadataService alleleNamesMetadataService,
            IAlleleGroupExpander alleleGroupExpander,
            ISmallGGroupToPGroupMetadataService smallGGroupToPGroupMetadataService)
            : base(hlaMetadataRepository, alleleNamesMetadataService)
        {
            this.alleleGroupExpander = alleleGroupExpander;
            this.smallGGroupToPGroupMetadataService = smallGGroupToPGroupMetadataService;
        }
        protected override async Task<List<string>> GetAlleleLookupNames(Locus locus, string lookupName, string hlaNomenclatureVersion)
        {
          var pGroup =  await smallGGroupToPGroupMetadataService.ConvertSmallGGroupToPGroup(locus, lookupName, hlaNomenclatureVersion);
          return (await alleleGroupExpander.ExpandAlleleGroup(locus, pGroup, hlaNomenclatureVersion)).ToList();
        }
    }
}
