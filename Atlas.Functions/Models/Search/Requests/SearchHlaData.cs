namespace Atlas.Functions.Models.Search.Requests
{
    public class SearchHlaData
    {
        /// <summary>
        /// Search HLA for locus A.
        /// Required.
        /// </summary>
        public LocusSearchHla LocusSearchHlaA { get; set; }

        /// <summary>
        /// Search HLA for locus B.
        /// Required.
        /// </summary>
        public LocusSearchHla LocusSearchHlaB { get; set; }

        /// <summary>
        /// Search HLA for locus C.
        /// Optional.
        /// </summary>
        public LocusSearchHla LocusSearchHlaC { get; set; }

        /// <summary>
        /// Search HLA for locus DPB1.
        /// Optional.
        /// </summary>
        public LocusSearchHla LocusSearchHlaDpb1 { get; set; }

        /// <summary>
        /// Search HLA for locus DQB1.
        /// Optional.
        /// </summary>
        public LocusSearchHla LocusSearchHlaDqb1 { get; set; }

        /// <summary>
        /// Search HLA for locus DRB1.
        /// Required.
        /// </summary>
        public LocusSearchHla LocusSearchHlaDrb1 { get; set; }
    }

    public class LocusSearchHla
    {
        /// <summary>
        /// String representation of the 1st search HLA type position at this locus.
        /// </summary>
        public string SearchHla1 { get; set; }

        /// <summary>
        /// String representation of the 2nd search HLA type position at this locus.
        /// </summary>
        public string SearchHla2 { get; set; }
    }

    public static class SearchHlaMappings
    {
        public static MatchingAlgorithm.Client.Models.SearchRequests.SearchHlaData ToMatchingAlgorithmSearchHla(this SearchHlaData hlaData)
        {
            return new MatchingAlgorithm.Client.Models.SearchRequests.SearchHlaData
            {
                LocusSearchHlaA = hlaData.LocusSearchHlaA.ToMatchingAlgorithmLocusSearchHla(),
                LocusSearchHlaB = hlaData.LocusSearchHlaB.ToMatchingAlgorithmLocusSearchHla(),
                LocusSearchHlaC = hlaData.LocusSearchHlaC.ToMatchingAlgorithmLocusSearchHla(),
                LocusSearchHlaDqb1 = hlaData.LocusSearchHlaDqb1.ToMatchingAlgorithmLocusSearchHla(),
                LocusSearchHlaDrb1 = hlaData.LocusSearchHlaDrb1.ToMatchingAlgorithmLocusSearchHla(),
            };
        }

        private static MatchingAlgorithm.Client.Models.SearchRequests.LocusSearchHla ToMatchingAlgorithmLocusSearchHla(
            this LocusSearchHla locusSearchHla)
        {
            return new MatchingAlgorithm.Client.Models.SearchRequests.LocusSearchHla
            {
                SearchHla1 = locusSearchHla.SearchHla1,
                SearchHla2 = locusSearchHla.SearchHla2
            };
        }
    }
}