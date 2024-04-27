using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;

namespace Atlas.MatchPrediction.Test.Validation.Models
{
    public class HomeworkRequest
    {
        /// <summary>
        /// Path where homework set file should be read from.
        /// Make sure to escape the backslashes in the path.
        /// </summary>
        public string InputPath { get; set; }

        /// <summary>
        /// Path where results should be written to.
        /// Make sure to escape the backslashes in the path.
        /// </summary>
        public string ResultsPath { get; set; }

        /// <summary>
        /// Include the file extension, e.g., "A.csv".
        /// </summary>
        public string SetFileName { get; set; }

        /// <summary>
        /// Loci set to `true` will be included in the analysis.
        /// </summary>
        public LociInfoTransfer<bool> MatchLoci { get; set; }
    }
}
