using System.Collections.Generic;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;
using Atlas.HlaMetadataDictionary.Repositories;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.HlaTypingInfo;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration
{
    internal interface IGGroupToPGroupService
    {
        IEnumerable<IGGroupToPGroupMetadata> GetGGroupToPGroupMetadata(string hlaNomenclatureVersion);
    }

    internal class GGroupToPGroupService : IGGroupToPGroupService
    {
        private readonly IWmdaDataRepository wmdaDataRepository;

        public GGroupToPGroupService(IWmdaDataRepository wmdaDataRepository, IHlaCategorisationService hlaCategorisationService)
        {
            this.wmdaDataRepository = wmdaDataRepository;
        }

        public IEnumerable<IGGroupToPGroupMetadata> GetGGroupToPGroupMetadata(string hlaNomenclatureVersion)
        {
            var dataset = wmdaDataRepository.GetWmdaDataset(hlaNomenclatureVersion);

            var gGroups = dataset.GGroups.Select(GetMetadataFromAlleleGroup);
            var pGroups = dataset.PGroups.Select(GetMetadataFromAlleleGroup);

            var locusToAllelesToPGroup = GetAllelesToPGroup(pGroups);

            return gGroups.Select(g => new GGroupToPGroupMetadata(g.Locus, g.LookupName, GetPGroup(locusToAllelesToPGroup, g)));
        }

        private static Dictionary<Locus, Dictionary<string, string>> GetAllelesToPGroup(IEnumerable<IAlleleGroupMetadata> pGroups)
        {
            var locusToAllelesToPGroup = new Dictionary<Locus, Dictionary<string, string>>();
            foreach (var pGroup in pGroups)
            {
                foreach (var allele in pGroup.AllelesInGroup)
                {
                    locusToAllelesToPGroup[pGroup.Locus] = new Dictionary<string, string>{{ allele, pGroup.LookupName }};
                }
            }

            return locusToAllelesToPGroup;
        }

        private static string GetPGroup(IReadOnlyDictionary<Locus, Dictionary<string, string>> locusToAllelesToPGroup, IAlleleGroupMetadata gGroups)
        {
            var allelesToPGroup = locusToAllelesToPGroup[gGroups.Locus];
            return (from alleles in gGroups.AllelesInGroup where allelesToPGroup.ContainsKey(alleles) select allelesToPGroup[alleles])
                .FirstOrDefault();
        }

        private static IAlleleGroupMetadata GetMetadataFromAlleleGroup(IWmdaAlleleGroup alleleGroup)
        {
            var locus = HlaMetadataDictionaryLoci.GetLocusFromTypingLocusNameIfExists(TypingMethod.Molecular, alleleGroup.TypingLocus);
            return new AlleleGroupMetadata(locus, alleleGroup.Name, alleleGroup.Alleles);
        }
    }
}
