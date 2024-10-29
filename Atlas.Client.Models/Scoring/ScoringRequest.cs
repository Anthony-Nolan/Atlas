using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using System.Collections.Generic;

namespace Atlas.Client.Models.Scoring
{
    public abstract class ScoringRequest
    {
        public PhenotypeInfoTransfer<string> PatientHla { get; set; }

        public ScoringCriteria ScoringCriteria { get; set;        }
    }

    public class DonorHlaScoringRequest : ScoringRequest
    {
        public PhenotypeInfoTransfer<string> DonorHla { get; set; }
    }

    /// <summary>
    /// Request to score multiple donors against a patient
    /// </summary>
    public class BatchScoringRequest : ScoringRequest
    {
        /// <summary>
        /// HLA of the donors to be scored
        /// </summary>
        public IEnumerable<IdentifiedDonorHla> DonorsHla { get; set; }
    }
}
