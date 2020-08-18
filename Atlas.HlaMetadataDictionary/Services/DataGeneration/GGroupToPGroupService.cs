using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;
using Atlas.HlaMetadataDictionary.Repositories;
using System.Linq;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
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
        private readonly ILogger logger;

        public GGroupToPGroupService(IWmdaDataRepository wmdaDataRepository, ILogger logger)
        {
            this.wmdaDataRepository = wmdaDataRepository;
            this.logger = logger;
        }

        public IEnumerable<IGGroupToPGroupMetadata> GetGGroupToPGroupMetadata(string hlaNomenclatureVersion)
        {
            var dataset = wmdaDataRepository.GetWmdaDataset(hlaNomenclatureVersion);

            var gGroups = dataset.GGroups.Select(GetMetadataFromAlleleGroup);
            var pGroups = dataset.PGroups.Select(GetMetadataFromAlleleGroup);

            var locusToAllelesToPGroup = GetLocusToAllelesToPGroup(pGroups);

            return gGroups.Select(g => new GGroupToPGroupMetadata(g.Locus, g.LookupName, GetPGroup(locusToAllelesToPGroup, g)));
        }

        private static Dictionary<Locus, Dictionary<string, string>> GetLocusToAllelesToPGroup(IEnumerable<IAlleleGroupMetadata> pGroups)
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

        private string GetPGroup(IReadOnlyDictionary<Locus, Dictionary<string, string>> locusToAllelesToPGroup, IAlleleGroupMetadata gGroups)
        {
            var allelesToPGroup = locusToAllelesToPGroup[gGroups.Locus];

            var pGroups = (from alleles in gGroups.AllelesInGroup
                where allelesToPGroup.ContainsKey(alleles)
                select allelesToPGroup[alleles]).ToList();

            if (pGroups.Count > 1)
            {
                const string errorMessage = "Encountered G Group with multiple corresponding P Groups. This is not expected to be possible.";
                logger.SendTrace(errorMessage, LogLevel.Error, new Dictionary<string, string> {{"GGroup", gGroups.LookupName}});
                throw new HlaMetadataDictionaryException(gGroups.Locus, gGroups.LookupName, errorMessage);
            } 

            return pGroups.FirstOrDefault();
        }

        private static IAlleleGroupMetadata GetMetadataFromAlleleGroup(IWmdaAlleleGroup alleleGroup)
        {
            var locus = HlaMetadataDictionaryLoci.GetLocusFromTypingLocusNameIfExists(TypingMethod.Molecular, alleleGroup.TypingLocus);
            return new AlleleGroupMetadata(locus, alleleGroup.Name, alleleGroup.Alleles);
        }
    }
}
