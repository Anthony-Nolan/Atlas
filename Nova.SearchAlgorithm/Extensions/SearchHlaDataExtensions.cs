using Nova.SearchAlgorithm.Client.Models.SearchRequests;
using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Extensions
{
    public static class SearchHlaDataExtensions
    {
        public static PhenotypeInfo<string> ToPhenotypeInfo(this SearchHlaData searchHlaData)
        {
            return new PhenotypeInfo<string>
            {
                A_1 = searchHlaData.LocusSearchHlaA.SearchHla1,
                A_2 = searchHlaData.LocusSearchHlaA.SearchHla2,
                B_1 = searchHlaData.LocusSearchHlaB.SearchHla1,
                B_2 = searchHlaData.LocusSearchHlaB.SearchHla2,
                C_1 = searchHlaData.LocusSearchHlaC?.SearchHla1,
                C_2 = searchHlaData.LocusSearchHlaC?.SearchHla2,
                Dqb1_1 = searchHlaData.LocusSearchHlaDqb1?.SearchHla1,
                Dqb1_2 = searchHlaData.LocusSearchHlaDqb1?.SearchHla2,
                Drb1_1 = searchHlaData.LocusSearchHlaDrb1.SearchHla1,
                Drb1_2 = searchHlaData.LocusSearchHlaDrb1.SearchHla2
            };
        }
    }
}