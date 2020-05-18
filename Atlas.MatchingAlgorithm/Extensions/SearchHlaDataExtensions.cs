using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Client.Models.SearchRequests;

namespace Atlas.MatchingAlgorithm.Extensions
{
    public static class SearchHlaDataExtensions
    {
        public static PhenotypeInfo<string> ToPhenotypeInfo(this SearchHlaData searchHlaData)
        {
            return new PhenotypeInfo<string>
            {
                A = { Position1 = searchHlaData.LocusSearchHlaA.SearchHla1, Position2 = searchHlaData.LocusSearchHlaA.SearchHla2},
                B = { Position1 = searchHlaData.LocusSearchHlaB.SearchHla1, Position2 = searchHlaData.LocusSearchHlaB.SearchHla2},
                Drb1 = { Position1 = searchHlaData.LocusSearchHlaDrb1.SearchHla1, Position2 = searchHlaData.LocusSearchHlaDrb1.SearchHla2},
                C = { Position1 = searchHlaData.LocusSearchHlaC?.SearchHla1, Position2 = searchHlaData.LocusSearchHlaC?.SearchHla2},
                Dpb1 = { Position1 = searchHlaData.LocusSearchHlaDpb1?.SearchHla1, Position2 = searchHlaData.LocusSearchHlaDpb1?.SearchHla2},
                Dqb1 = { Position1 = searchHlaData.LocusSearchHlaDqb1?.SearchHla1, Position2 = searchHlaData.LocusSearchHlaDqb1?.SearchHla2},
            };
        }
    }
}