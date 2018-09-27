namespace Nova.SearchAlgorithm.MatchingDictionary.Properties
{
    public static class StaticFeatureFlags
    {
        /// <summary>
        /// When calculating scoring data, when true we will consider single null alleles for scoring, but not in other scenarios
        /// e.g. allele strings, NMDP codes, XX codes that include null alleles
        ///
        /// There is little value to having this feature flag be configurable at runtime, as the matching dictionary will need regenerating if this is changed.
        ///
        /// Note that this flag is static as it is used by a class that requires a parameterless constructor
        /// </summary>
        public static bool ShouldIgnoreNullAllelesInAlleleStrings { get; } = true;
    }
}