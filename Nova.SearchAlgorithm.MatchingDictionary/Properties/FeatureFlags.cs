namespace Nova.SearchAlgorithm.MatchingDictionary.Properties
{
    public interface IFeatureFlags
    {
        /// <summary>
        /// When calculating scoring data, when true we will consider single null alleles for scoring, but not in other scenarios
        /// e.g. allele strings, NMDP codes, XX codes that include null alleles
        ///
        /// There is little value to having this feature flag be configurable at runtime, as the matching dictionary will need regenerating if this is changed.
        /// </summary>
        bool ShouldIgnoreNullAllelesInAlleleStrings { get; }
    }
    
    public class FeatureFlags: IFeatureFlags
    {
        public bool ShouldIgnoreNullAllelesInAlleleStrings { get; } = true;
    }
}