using System;
using System.Collections.Generic;

namespace Atlas.Client.Models.Search.Results.Matching.PerLocus
{
    public enum MatchGrade
    {
        /// Mismatch grades
        Mismatch,
        PermissiveMismatch,

        // Grades for Serology-level matches
        Broad,
        Split,
        Associated,

        // Grades for Null vs. Null allele matches
        NullMismatch,
        NullPartial,
        NullCDna,
        NullGDna,

        // Grades for Expressing vs. Expressing allele matches
        PGroup,
        GGroup,
        Protein,
        CDna,
        GDna,
        
        /// <summary>
        /// The Match Grade cannot be known. Note that for most loci, untyped HLA will be considered a "PGroup" level match - only DPB1 will ever be classed as "Unknown".
        /// </summary>
        Unknown,
    }

    public static class MatchGradeConstants
    {
        /// <summary>
        /// Collection of all Match Grades that can be considered a "match"
        /// </summary>
        public static readonly HashSet<MatchGrade> MatchingGrades = new HashSet<MatchGrade>
        {
            MatchGrade.GDna,
            MatchGrade.CDna,
            MatchGrade.Protein,
            MatchGrade.GGroup,
            MatchGrade.PGroup,
            MatchGrade.NullGDna,
            MatchGrade.NullCDna,
            MatchGrade.NullPartial,
            MatchGrade.Associated,
            MatchGrade.Broad,
            MatchGrade.Split
        };

        /// <summary>
        /// Collection of all Match Grades that can be considered a "mismatch"
        /// </summary>
        public static readonly HashSet<MatchGrade> MismatchGrades = new HashSet<MatchGrade>
        {
            MatchGrade.NullMismatch,
            MatchGrade.PermissiveMismatch,
            MatchGrade.Mismatch
        };
    }
    
    public static class MatchGradeExtensions
    {
        public static LocusMatchCategory ToLocusMatchCategory(this MatchGrade matchGrade)
        {
            return matchGrade switch
            {
                MatchGrade.GDna => LocusMatchCategory.Match,
                MatchGrade.CDna => LocusMatchCategory.Match,
                MatchGrade.Protein => LocusMatchCategory.Match,
                MatchGrade.GGroup => LocusMatchCategory.Match,
                MatchGrade.PGroup => LocusMatchCategory.Match,
                MatchGrade.NullGDna => LocusMatchCategory.Match,
                MatchGrade.NullCDna => LocusMatchCategory.Match,
                MatchGrade.NullPartial => LocusMatchCategory.Match,
                MatchGrade.Associated => LocusMatchCategory.Match,
                MatchGrade.Broad => LocusMatchCategory.Match,
                MatchGrade.Split => LocusMatchCategory.Match,
                MatchGrade.PermissiveMismatch => LocusMatchCategory.PermissiveMismatch,
                MatchGrade.Mismatch => LocusMatchCategory.Mismatch,
                MatchGrade.NullMismatch => LocusMatchCategory.Mismatch,
                MatchGrade.Unknown => LocusMatchCategory.Unknown,
                _ => throw new ArgumentOutOfRangeException($"Cannot convert MatchGrade {matchGrade} to a LocusMatchCategory")
            };
        }
    }
}