﻿using Atlas.Client.Models.Search.Results.Matching.PerLocus;

namespace Atlas.Client.Models.Common.Results
{
    /// <summary>
    /// Scoring results for a given locus.
    /// 
    /// Note: Position scores held in <see cref="ScoreDetailsAtPositionOne"/> and <see cref="ScoreDetailsAtPositionTwo"/> are orientated w.r.t donor typing.
    /// E.g., Given a locus with a single mismatch where patient typing 1 is mismatched to donor typing 2, <see cref="ScoreDetailsAtPositionTwo"/> will be assigned the mismatch score.
    /// </summary>
    public class LocusSearchResult
    {
        /// <summary>
        ///     Determined by whether matching was requested for this locus or not.
        ///     e.g. In a 6/6 search, loci C and DQB1 will have their individual match counts populated via the scoring process,
        ///     and these match grades will not be included in the total Match Count in the api response
        /// </summary>
        public bool IsLocusMatchCountIncludedInTotal { get; set; }

        /// <summary>
        /// The overall, aggregated, match category for this locus.
        /// </summary>
        public LocusMatchCategory? MatchCategory { get; set; }

        /// <summary>
        ///     The number of matches within this locus.
        ///     Either 0, 1 or 2 if the locus is typed.
        ///     If the locus is not typed this will be 2, since there is a potential match.
        ///     If matching and/or scoring has not been requested on the locus, then the value will be null.
        /// </summary>
        public int? MatchCount { get; set; }

        /// <summary>
        ///     Reports whether this locus is typed for the given donor.
        ///     If scoring has not been requested on the locus, then the value will be null.
        /// </summary>
        public bool? IsLocusTyped { get; set; }

        /// <summary>
        ///     A numeric value representing the relative match grade at this locus, according to the scoring algorithm
        ///     If scoring has not been requested on the locus, then the value will be null.
        /// </summary>
        public int? MatchGradeScore { get; set; }

        /// <summary>
        ///     A numeric value representing the relative match confidence at this locus, according to the scoring algorithm
        ///     If scoring has not been requested on the locus, then the value will be null.
        /// </summary>
        public int? MatchConfidenceScore { get; set; }

        /// <summary>
        ///     If scoring has not been requested on the locus, then the value will be null.
        /// </summary>
        public LocusPositionScoreDetails ScoreDetailsAtPositionOne { get; set; }

        /// <summary>
        ///     If scoring has not been requested on the locus, then the value will be null.
        /// </summary>
        public LocusPositionScoreDetails ScoreDetailsAtPositionTwo { get; set; }

        /// <summary>
        /// Indicates the direction of the mismatch, when there is a mismatch.
        /// When the mismatch is permissive or there is no mismatch, NotApplicable will be returned.
        /// When the direction could not be calculated or the locus is non-DPB1, null will be returned.
        /// Currently the mismatch direction is only implemented for DPB1 loci.
        /// </summary>
        public MismatchDirection? MismatchDirection { get; set; }

    }
}