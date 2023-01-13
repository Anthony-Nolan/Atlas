using System.Collections.Generic;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.Test.Integration.Resources.Alleles
{
    /// <summary>
    /// Provides collections of valid GGroups at each supported locus.
    /// No relations are implied between these collections
    /// </summary>
    internal static class AlleleGroups
    {
        /// <summary>
        /// GGroups selected such that none share P-Groups
        /// </summary>
        public static readonly LociInfo<IReadOnlyCollection<string>> GGroups = new LociInfo<IReadOnlyCollection<string>>
        (
            valueA: new List<string>
            {
                "01:01:01G",
                "01:11N",
                "02:01:02G",
            },
            valueB: new List<string>
            {
                "08:44",
                "08:53:01",
                "13:01:01G",
            },
            valueC: new List<string>
            {
                "03:263:01",
                "03:277N",
                "04:01:01G",
            },
            valueDqb1: new List<string>
            {
                "06:02:01G",
                "06:158N",
                "03:154",
            },
            valueDrb1: new List<string>
            {
                "03:07:01G",
                "03:08",
                "03:25:01",
                "04:02:01G",
            }
        );
    }
}