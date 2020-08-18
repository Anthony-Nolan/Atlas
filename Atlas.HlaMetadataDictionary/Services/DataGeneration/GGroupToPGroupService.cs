using System;
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

            return gGroups.Select(gGroup => GetMetadata(locusToAllelesToPGroup, gGroup));
        }

        private Dictionary<Tuple<Locus, string>, string> GetLocusToAllelesToPGroup(IEnumerable<IAlleleGroupMetadata> pGroups)
        {
            var locusToAllelesToPGroup = new Dictionary<Tuple<Locus, string>, string>();

            foreach (var pGroup in pGroups)
            {
                foreach (var allele in pGroup.AllelesInGroup)
                {
                    if (locusToAllelesToPGroup.ContainsKey(new Tuple<Locus, string>(pGroup.Locus, allele)))
                    {
                        const string errorMessage = "Encountered allele at locus with multiple corresponding P Groups. This is not expected to be possible.";
                        logger.SendTrace(errorMessage, LogLevel.Error, new Dictionary<string, string> {{"Allele", allele}});
                        throw new HlaMetadataDictionaryException(pGroup.Locus, allele, errorMessage);
                    }

                    locusToAllelesToPGroup.Add(new Tuple<Locus, string>(pGroup.Locus, allele), pGroup.LookupName);
                }
            }

            return locusToAllelesToPGroup;
        }

        private static IGGroupToPGroupMetadata GetMetadata(IReadOnlyDictionary<Tuple<Locus, string>, string> locusToAllelesToPGroup, IAlleleGroupMetadata gGroups)
        {
            var pGroup = gGroups.AllelesInGroup.Where(allele => 
                    locusToAllelesToPGroup.ContainsKey(new Tuple<Locus, string>(gGroups.Locus, allele)))
                .Select(allele => locusToAllelesToPGroup[new Tuple<Locus, string>(gGroups.Locus, allele)])
                .FirstOrDefault();

            return new GGroupToPGroupMetadata(gGroups.Locus, gGroups.LookupName, pGroup);
        }

        private static IAlleleGroupMetadata GetMetadataFromAlleleGroup(IWmdaAlleleGroup alleleGroup)
        {
            var locus = HlaMetadataDictionaryLoci.GetLocusFromTypingLocusNameIfExists(TypingMethod.Molecular, alleleGroup.TypingLocus);
            return new AlleleGroupMetadata(locus, alleleGroup.Name, alleleGroup.Alleles);
        }
    }
}
