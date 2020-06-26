﻿using Atlas.Common.GeneticData;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval.Lookups
{
    internal class AlleleGroupLookup : AlleleNamesLookupBase
    {
        private readonly IAlleleGroupExpander alleleGroupExpander;

        public AlleleGroupLookup(
            IHlaMetadataRepository hlaMetadataRepository,
            IAlleleNamesMetadataService alleleNamesMetadataService,
            IAlleleGroupExpander alleleGroupExpander)
            : base(hlaMetadataRepository, alleleNamesMetadataService)
        {
            this.alleleGroupExpander = alleleGroupExpander;
        }

        protected override async Task<IEnumerable<string>> GetAlleleLookupNames(Locus locus, string lookupName, string hlaNomenclatureVersion)
        {
            return await alleleGroupExpander.ExpandAlleleGroup(locus, lookupName, hlaNomenclatureVersion);
        }
    }
}