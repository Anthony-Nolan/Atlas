using System.ComponentModel.DataAnnotations;

namespace Atlas.MatchPrediction.ExternalInterface.Settings
{
    /// <summary>
    /// Settings controlling genotype imputation, shared across every host that runs the imputation code path
    /// (search-time durable-function path, the ACA parallel-batch worker, and - once precompute lands - Data Refresh
    /// and Donor Management). The configured value is expected to be identical across those hosts, fanned out from a
    /// single Terraform variable (see terraform/core/variables.tf).
    /// </summary>
    public class GenotypeImputationSettings
    {
        /// <summary>
        /// The maximum number of expanded genotypes to retain per input (patient/donor) before match calculation.
        /// See <see cref="Atlas.MatchPrediction.Services.MatchProbability.ExpandedGenotypeTruncater"/> for the accuracy
        /// vs. performance/memory trade-off this value represents. Defaults to 2000 so hosts that have not (yet) had the
        /// setting configured continue to use the historically hardcoded value.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int MaximumExpandedGenotypesPerInput { get; set; } = 2000;
    }
}
