namespace Atlas.MatchPrediction.ExternalInterface.Settings
{
    public class MatchPredictionAlgorithmSettings
    {
        /// <summary>
        /// When true, HLA metadata dictionary (HMD) exceptions thrown during compressed phenotype conversion will be caught and suppressed.
        /// The most common usage scenario for error suppression is within search:
        /// subject HLA will have been validated by the matching algorithm component before reaching match prediction.
        /// Thus, the only reason for a conversion error during phenotype conversion is due to the matching algorithm and
        /// haplotype frequency set being on different nomenclature versions.
        /// See https://github.com/Anthony-Nolan/Atlas/issues/636 for more info.
        /// </summary>
        public bool SuppressCompressedPhenotypeConversionExceptions { get; set; }
    }
}
