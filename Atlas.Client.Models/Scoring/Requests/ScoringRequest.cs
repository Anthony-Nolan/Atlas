using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using System.Collections.Generic;
using Atlas.Client.Models.Common.Requests;

namespace Atlas.Client.Models.Scoring.Requests
{
    public abstract class ScoringRequest
    {
        public PhenotypeInfoTransfer<string> PatientHla { get; set; }

        public ScoringCriteria ScoringCriteria { get; set; }
    }

    public class DonorHlaScoringRequest : ScoringRequest
    {
        public PhenotypeInfoTransfer<string> DonorHla { get; set; }
    }

    /// <summary>
    /// Request to score multiple donors against a patient
    /// </summary>
    public class DonorHlaBatchScoringRequest : ScoringRequest
    {
        /// <summary>
        /// HLA of the donors to be scored
        /// </summary>
        public IEnumerable<IdentifiedDonorHla> DonorsHla { get; set; }
    }

    public class IdentifiedDonorHla : PhenotypeInfoTransfer<string>
    {
        public string DonorId { get; set; }
    }
}
