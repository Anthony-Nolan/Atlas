locals {
  // Settings for the genotype set pipeline that runs during differential donor import. Shared by the Donor Management
  // and Data Refresh apps so that both use identical values, and the same ones as the Match Prediction hosts - if the
  // truncation limit differed, stored results would not match live results.
  match_prediction_func_app_settings = {
    "GenotypeImputation:MaximumExpandedGenotypesPerInput" = var.MATCH_PREDICTION_MAX_EXPANDED_GENOTYPES_PER_INPUT

    "HaplotypeFrequencySetCache:ActiveSetCacheExpiryMinutes" = var.MATCH_PREDICTION_ACTIVE_HF_SET_CACHE_EXPIRY_MINUTES
    "HaplotypeFrequencySetCache:MaxCachedFrequencySets"      = var.MATCH_PREDICTION_MAX_CACHED_FREQUENCY_SETS
    "HaplotypeFrequencySetCache:SetCacheExpiryMinutes"       = var.MATCH_PREDICTION_SET_CACHE_EXPIRY_MINUTES
  }
}
