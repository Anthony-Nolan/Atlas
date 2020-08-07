using System.Collections.Generic;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;

namespace Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability
{
    public class MatchProbabilityInput
    {
        /// <summary>
        /// Donor ID is not strictly necessary for running match prediction, but will be useful for logging
        /// </summary>
        public int DonorId { get; set; }

        /// <summary>
        /// Search ID is not necessary for running match prediction, but will be useful for logging
        /// </summary>
        public string SearchRequestId { get; set; }

        /// <summary>
        /// Match prediction will be run on all loci by default.
        /// Any loci specified here will be ignored at all stages of match probability calculation, and will not have per-locus predictions returned.
        /// </summary>
        public IEnumerable<Locus> ExcludedLoci { get; set; } = new List<Locus>();

        public PhenotypeInfoTransfer<string> DonorHla { get; set; }
        public FrequencySetMetadata DonorFrequencySetMetadata { get; set; }
        public PhenotypeInfoTransfer<string> PatientHla { get; set; }
        public FrequencySetMetadata PatientFrequencySetMetadata { get; set; }
        public string HlaNomenclatureVersion { get; set; }
    }
}