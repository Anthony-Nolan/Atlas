using System.Collections.Generic;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;

namespace Atlas.MatchPrediction.ApplicationInsights
{
    internal class MatchPredictionLoggingContext : LoggingContext
    {
        public void Initialise(MatchProbabilityInput matchProbabilityInput)
        {
            SearchRequestId = matchProbabilityInput.SearchRequestId;
            HlaNomenclatureVersion = matchProbabilityInput.HlaNomenclatureVersion;
            DonorId = matchProbabilityInput.DonorId.ToString();
            DonorHla = matchProbabilityInput.DonorHla;
            PatientHla = matchProbabilityInput.PatientHla;
        }

        public string SearchRequestId { get; set; }
        public string HlaNomenclatureVersion { get; set; }
        public string DonorId { get; set; }
        public PhenotypeInfo<string> DonorHla { get; set; }
        public PhenotypeInfo<string> PatientHla { get; set; }

        /// <inheritdoc />
        public override Dictionary<string, string> PropertiesToLog()
        {
            return new Dictionary<string, string>
            {
                {nameof(SearchRequestId), SearchRequestId},
                {nameof(HlaNomenclatureVersion), HlaNomenclatureVersion},
                {nameof(DonorId), DonorId},
                {nameof(DonorHla), DonorHla?.PrettyPrint()},
                {nameof(PatientHla), PatientHla?.PrettyPrint()}
            };
        }
    }
}