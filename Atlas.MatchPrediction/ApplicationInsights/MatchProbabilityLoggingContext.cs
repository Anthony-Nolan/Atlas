using System.Collections.Generic;
using System.Linq;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;

namespace Atlas.MatchPrediction.ApplicationInsights
{
    public class MatchProbabilityLoggingContext : LoggingContext
    {
        public string SearchRequestId { get; set; }
        public string MatchingAlgorithmHlaNomenclatureVersion { get; set; }
        public string DonorIds { get; set; }
        public PhenotypeInfo<string> DonorHla { get; set; }
        public PhenotypeInfo<string> PatientHla { get; set; }

        public void Initialise(SingleDonorMatchProbabilityInput singleDonorMatchProbabilityInput)
        {
            SearchRequestId = singleDonorMatchProbabilityInput.SearchRequestId;
            DonorIds = singleDonorMatchProbabilityInput.Donor?.DonorIds?.Select(id => id.ToString()).StringJoin(",");
            DonorHla = singleDonorMatchProbabilityInput.Donor?.DonorHla?.ToPhenotypeInfo();
            PatientHla = singleDonorMatchProbabilityInput.PatientHla?.ToPhenotypeInfo();
        }

        /// <inheritdoc />
        public override Dictionary<string, string> PropertiesToLog()
        {
            return new Dictionary<string, string>
            {
                {nameof(SearchRequestId), SearchRequestId},
                {nameof(MatchingAlgorithmHlaNomenclatureVersion), MatchingAlgorithmHlaNomenclatureVersion},
                {nameof(DonorIds), DonorIds},
                {nameof(DonorHla), DonorHla?.PrettyPrint()},
                {nameof(PatientHla), PatientHla?.PrettyPrint()}
            };
        }
    }
}