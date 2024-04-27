using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using System.Collections.Generic;

namespace Atlas.MatchPrediction.Test.Validation.Models
{
    public class HomeworkRequest
    {
        /// <summary>
        /// Each string should have format: "PatientId,DonorId"
        /// </summary>
        public IEnumerable<string> PatientDonorPairs { get; set; }

        /// <summary>
        /// Path where results should be written to.
        /// Make sure to escape the backslashes in the path.
        /// </summary>
        public string ResultsPath { get; set; }

        /// <summary>
        /// Used to identify the result set.
        /// </summary>
        public string HomeworkSetName { get; set; }

        /// <summary>
        /// Loci set to `true` will be included in the analysis.
        /// </summary>
        public LociInfoTransfer<bool> MatchLoci { get; set; }
    }
}
