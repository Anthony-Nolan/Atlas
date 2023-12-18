using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Atlas.Client.Models.Search.Results.Matching.PerLocus
{
    /// <summary>
    /// Grades assigned during the scoring stage.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum MatchGrade
    {
        /// <summary>
        /// Allele-level mismatch
        /// </summary>
        Mismatch,

        /// <summary>
        /// One typing is expressing and the other is a null-expressing allele.
        /// It is not matched nor mismatched but is a special case.
        /// It has a lower value than other "matching" grades, especially Null vs. Null grades, to ensure they are assigned in preference to this grade.
        /// </summary>
        ExpressingVsNull,

        /// <summary>
        /// "SI3" serology match
        /// </summary>
        Broad,

        /// <summary>
        /// "SI2" serology match
        /// </summary>
        Split,

        /// <summary>
        /// "SI1" serology match
        /// </summary>
        Associated,

        /// <summary>
        /// Both null alleles have different sequences
        /// </summary>
        NullMismatch,

        /// <summary>
        /// Both null alleles have same name AND have partial g/cDNA
        /// </summary>
        NullPartial,

        /// <summary>
        /// Both null alleles have same nucleotide sequence across coding regions only
        /// </summary>
        NullCDna,

        /// <summary>
        /// Both null alleles have same nucleotide sequence across entire gene
        /// </summary>
        NullGDna,

        /// <summary>
        /// Both typings share same polypeptide sequence across ABD only
        /// </summary>
        PGroup,

        /// <summary>
        /// Both typings share same nucleotide sequence across ABD only
        /// </summary>
        GGroup,

        /// <summary>
        /// Both typings share same polypeptide sequence across coding regions only
        /// </summary>
        Protein,

        /// <summary>
        /// Both typings share same nucleotide sequence across coding regions only
        /// </summary>
        CDna,

        /// <summary>
        /// Both typings share same nucleotide sequence across entire gene
        /// </summary>
        GDna,
        
        /// <summary>
        /// The match grade cannot be known.
        /// Note that for most loci, untyped HLA will be graded as potentially <see cref="PGroup"/> - only DPB1 will ever be classed as <see cref="Unknown"/>.
        /// </summary>
        Unknown,
    }

    [Obsolete($"Class is not referenced within the codebase, is not maintained with changes to {nameof(MatchGrade)}, and will be removed.")]
    public static class MatchGradeConstants
    {
        /// <summary>
        /// Collection of all Match Grades that can be considered a "match"
        /// </summary>
        public static readonly HashSet<MatchGrade> MatchingGrades = new()
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
        public static readonly HashSet<MatchGrade> MismatchGrades = new()
        {
            MatchGrade.NullMismatch,
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

                // This is not technically a match, but it is not a mismatch either.
                // Assigning the mapping of "Match" allows the grade of the expressing typing within the same locus
                // to determine the overall match category, as currently the worst of the two positional categories is used.
                // I.e., if the expressing typing is a match, then the overall locus match category will be "Match",
                // and if the expressing typing is a mismatch, then the overall locus match category will be "Mismatch".
                MatchGrade.ExpressingVsNull => LocusMatchCategory.Match,

                MatchGrade.Mismatch => LocusMatchCategory.Mismatch,
                MatchGrade.NullMismatch => LocusMatchCategory.Mismatch,
                MatchGrade.Unknown => LocusMatchCategory.Unknown,
                _ => throw new ArgumentOutOfRangeException($"Cannot convert MatchGrade {matchGrade} to a LocusMatchCategory")
            };
        }
    }
}