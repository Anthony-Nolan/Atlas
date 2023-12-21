using Atlas.Client.Models.Search;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;

namespace Atlas.MatchPrediction.Test.Validation.Models
{
    public class ValidationSearchRequest
    {
        public DonorType DonorType { get; set; }
        public int MismatchCount { get; set; }

        /// <summary>
        /// Loci set to `true` will be included in match criteria.
        /// </summary>
        public LociInfoTransfer<bool> MatchLoci { get; set; }
    }
}
