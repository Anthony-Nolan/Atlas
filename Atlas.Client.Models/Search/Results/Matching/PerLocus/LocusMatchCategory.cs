﻿namespace Atlas.Client.Models.Search.Results.Matching.PerLocus
{
    /// <summary>
    /// This enum represents the same type of data as <see cref="Atlas.Client.Models.Search.Results.Matching.MatchCategory"/>,
    /// but in a per-locus context (aggregated from the locus' data) rather then aggregated over the whole donor.
    /// </summary>
    public enum LocusMatchCategory
    {
        Match,
        PermissiveMismatch,
        Mismatch,
        Unknown
    }
}