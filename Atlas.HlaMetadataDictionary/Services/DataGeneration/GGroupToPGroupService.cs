using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;
using Atlas.HlaMetadataDictionary.Repositories;
using System.Linq;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.HlaMetadataDictionary.HlaTypingInfo;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration
{
    internal interface IGGroupToPGroupService
    {
        IEnumerable<IMolecularTypingToPGroupMetadata> GetGGroupToPGroupMetadata(string hlaNomenclatureVersion);
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

        public IEnumerable<IMolecularTypingToPGroupMetadata> GetGGroupToPGroupMetadata(string hlaNomenclatureVersion)
        {
            var dataset = wmdaDataRepository.GetWmdaDataset(hlaNomenclatureVersion);

            var gGroups = dataset.GGroups.Select(GetMetadataFromAlleleGroup);
            var pGroups = dataset.PGroups.Select(GetMetadataFromAlleleGroup);

            var alleleToPGroup = BuildAlleleToPGroupDictionary(pGroups);

            return gGroups.Select(gGroup => GetMetadata(alleleToPGroup, gGroup));
        }

        private static Dictionary<(Locus, string), string> BuildAlleleToPGroupDictionary(IEnumerable<IAlleleGroupMetadata> pGroups)
        {
            return pGroups.SelectMany(pGroup =>
            {
                var keys = pGroup.AllelesInGroup.Select(allele => (pGroup.Locus, allele));
                return keys.ToDictionary(k => k, _ => pGroup.LookupName);
            }).ToDictionary();
        }

        private IMolecularTypingToPGroupMetadata GetMetadata(IReadOnlyDictionary<(Locus, string), string> alleleToPGroup, IAlleleGroupMetadata gGroup)
        {
            // GGroup alleles with no PGroup are expected, as in the case of null expressing alleles only.
            var pGroup = gGroup.AllelesInGroup.Where(allele =>
                    alleleToPGroup.ContainsKey((gGroup.Locus, allele)))
                .Select(allele => alleleToPGroup[(gGroup.Locus, allele)])
                .Distinct().ToList();

            if (pGroup.Count > 1)
            {
                const string errorMessage = "Encountered G Group at locus with multiple corresponding P Groups. This is not expected to be possible.";
                logger.SendTrace(errorMessage, LogLevel.Error, new Dictionary<string, string> {{"G Group", gGroup.LookupName}});
                throw new HlaMetadataDictionaryException(gGroup.Locus, gGroup.LookupName, errorMessage);
            }

            return new MolecularTypingToPGroupMetadata(gGroup.Locus, gGroup.LookupName, pGroup.SingleOrDefault());
        }

        private static IAlleleGroupMetadata GetMetadataFromAlleleGroup(IWmdaAlleleGroup alleleGroup)
        {
            var locus = HlaMetadataDictionaryLoci.GetLocusFromTypingLocusNameIfExists(TypingMethod.Molecular, alleleGroup.TypingLocus);
            return new AlleleGroupMetadata(locus, alleleGroup.Name, alleleGroup.Alleles);
        }
    }
}
