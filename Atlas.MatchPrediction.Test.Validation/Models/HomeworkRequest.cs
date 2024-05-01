using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;

namespace Atlas.MatchPrediction.Test.Validation.Models
{
    public class HomeworkRequest
    {
        /// <summary>
        /// Path where homework set files should be read from.
        /// Make sure to escape the backslashes in the path.
        /// </summary>
        public string InputPath { get; set; }

        /// <summary>
        /// Loci set to `true` will be included in the analysis.
        /// </summary>
        public LociInfoTransfer<bool> MatchLoci { get; set; }

        /// <summary>
        /// E.g., "3520".
        /// </summary>
        public string MatchingAlgorithmHlaNomenclatureVersion { get; set; }

        /// <summary>
        /// If `true`, all previously stored homework sets will be deleted from the validation database.
        /// </summary>
        public bool DeletePreviousHomeworkSets { get; set; }
    }
}
