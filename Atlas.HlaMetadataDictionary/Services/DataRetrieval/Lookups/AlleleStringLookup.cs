using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Utils.Http;
using Atlas.HlaMetadataDictionary.InternalExceptions;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval.Lookups
{
    internal class AlleleStringLookup : AlleleNamesLookupBase
    {
        private readonly IAlleleNamesExtractor alleleNamesExtractor;
        
        public AlleleStringLookup(
            IHlaMetadataRepository hlaMetadataRepository,
            IAlleleNamesMetadataService alleleNamesMetadataService,
            IAlleleNamesExtractor alleleNamesExtractor)
            : base(hlaMetadataRepository, alleleNamesMetadataService)
        {
            this.alleleNamesExtractor = alleleNamesExtractor;
        }

        protected override async Task<List<string>> GetAlleleLookupNames(Locus locus, string lookupName, string hlaNomenclatureVersion)
        {
            try
            {
                return await Task.Run(() => alleleNamesExtractor.GetAlleleNamesFromAlleleString(lookupName).ToList());
            }
            // The extractor re-categorises the string and splits it, and reports a string it cannot use in the
            // vocabulary of whichever step objected: AtlasHttpException from the categoriser, ArgumentException from
            // the splitter for a string that does not hold two well-formed alleles. Both mean the same thing here -
            // this name has no data - and neither is an HlaMetadataDictionaryException, so without this an unusable
            // allele string would leave as an infrastructure fault. Same reasoning as MacLookup.
            catch (Exception e) when (e is AtlasHttpException or ArgumentException)
            {
                throw new InvalidHlaException(locus, lookupName);
            }
        }
    }
}
