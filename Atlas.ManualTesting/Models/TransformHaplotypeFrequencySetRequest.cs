using Atlas.Common.Public.Models.GeneticData;

namespace Atlas.ManualTesting.Models
{
    public class FindReplaceHlaNames
    {
        /// <summary>
        /// Locus for <see cref="TargetHlaName"/>
        /// </summary>
        public Locus Locus { get; set; }

        /// <summary>
        /// HLA name that should be replaced with <see cref="ReplacementHlaName"/>, exactly as it appears within the file.
        /// </summary>
        public string TargetHlaName { get; set; }

        /// <summary>
        /// HLA name which will replace <see cref="TargetHlaName"/>
        /// </summary>
        public string ReplacementHlaName { get; set; }
    }

    public class TransformHaplotypeFrequencySetRequest
    {
        /// <summary>
        /// Path to Haplotype Frequency file
        /// </summary>
        public string HaplotypeFrequencySetFilePath { get; set; }

        public FindReplaceHlaNames FindReplaceHlaNames { get; set; }
    }
}
