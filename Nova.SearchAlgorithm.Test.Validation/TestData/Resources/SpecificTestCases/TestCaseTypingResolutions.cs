using System.Collections.Generic;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Resources.SpecificTestCases
{
    public static class TestCaseTypingResolutions
    {
        /// <summary>
        /// Used in test cases where a 'different' set of hla data requested.
        /// This is different from an arbitrary set as we should guarantee a different resolution at each locus.
        /// </summary>
        public static readonly Dictionary<Locus, HlaTypingResolution> DifferentLociResolutions = new Dictionary<Locus, HlaTypingResolution>
        {
            {Locus.A, HlaTypingResolution.Tgs},
            {Locus.B, HlaTypingResolution.NmdpCode},
            {Locus.C, HlaTypingResolution.Serology},
            {Locus.Dpb1, HlaTypingResolution.TwoFieldTruncatedAllele},
            {Locus.Dqb1, HlaTypingResolution.XxCode},
            {Locus.Drb1, HlaTypingResolution.ThreeFieldTruncatedAllele},
        };
    }
}