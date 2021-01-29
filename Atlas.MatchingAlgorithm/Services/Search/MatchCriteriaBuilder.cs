using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Mapping;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;

namespace Atlas.MatchingAlgorithm.Services.Search
{
    internal interface IMatchCriteriaBuilder
    {
        Task<AlleleLevelMatchCriteria> BuildAlleleLevelMatchCriteria(SearchRequest matchingRequest);
    }

    internal class MatchCriteriaBuilder : IMatchCriteriaBuilder
    {
        private readonly IHlaMetadataDictionary hlaMetadataDictionary;

        public MatchCriteriaBuilder(
            IHlaMetadataDictionaryFactory factory,
            IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor)
        {
            hlaMetadataDictionary = factory.BuildDictionary(hlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersion());
        }

        public async Task<AlleleLevelMatchCriteria> BuildAlleleLevelMatchCriteria(SearchRequest matchingRequest)
        {
            var matchCriteria = matchingRequest.MatchCriteria;
            var searchHla = matchingRequest.SearchHlaData.ToPhenotypeInfo();

            var criteriaMappings = await Task.WhenAll(
                MapLocusInformationToMatchCriteria(Locus.A, matchCriteria.LocusMismatchCriteria.A, searchHla.A),
                MapLocusInformationToMatchCriteria(Locus.B, matchCriteria.LocusMismatchCriteria.B, searchHla.B),
                MapLocusInformationToMatchCriteria(Locus.C, matchCriteria.LocusMismatchCriteria.C, searchHla.C),
                MapLocusInformationToMatchCriteria(Locus.Dqb1, matchCriteria.LocusMismatchCriteria.Dqb1, searchHla.Dqb1),
                MapLocusInformationToMatchCriteria(Locus.Drb1, matchCriteria.LocusMismatchCriteria.Drb1, searchHla.Drb1));

            return new AlleleLevelMatchCriteria
            {
                SearchType = matchingRequest.SearchDonorType.ToMatchingAlgorithmDonorType(),
                DonorMismatchCount = matchCriteria.DonorMismatchCount,
                LocusCriteria = new LociInfo<AlleleLevelLocusMatchCriteria>(
                    criteriaMappings[0],
                    criteriaMappings[1],
                    criteriaMappings[2],
                    null,
                    criteriaMappings[3],
                    criteriaMappings[4]
                ),
            };
        }

        private async Task<AlleleLevelLocusMatchCriteria> MapLocusInformationToMatchCriteria(
            Locus locus,
            int? allowedMismatches,
            LocusInfo<string> searchHla)
        {
            if (allowedMismatches == null)
            {
                return null;
            }

            var searchTerm = new LocusInfo<string>(searchHla.Position1, searchHla.Position2);

            var metadata = await hlaMetadataDictionary.GetLocusHlaMatchingMetadata(
                locus,
                searchTerm
            );

            return new AlleleLevelLocusMatchCriteria
            {
                MismatchCount = allowedMismatches.Value,
                PGroupsToMatchInPositionOne = metadata.Position1.MatchingPGroups,
                PGroupsToMatchInPositionTwo = metadata.Position2.MatchingPGroups
            };
        }
    }
}