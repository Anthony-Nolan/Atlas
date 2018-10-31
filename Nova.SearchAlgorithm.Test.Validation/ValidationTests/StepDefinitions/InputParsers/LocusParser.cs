using System;
using System.Collections.Generic;
using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Test.Validation.ValidationTests.StepDefinitions.InputParsers
{
    public static class LocusParser
    {
        public static IEnumerable<Locus> ParseLoci(string locusType)
        {
            switch (locusType)
            {
                case "each locus":
                case "all loci":
                    return LocusHelpers.AllLoci();
                case "locus A":
                    return new[] {Locus.A};
                case "locus B":
                    return new[] {Locus.B};
                case "locus C":
                    return new[] {Locus.C};
                case "locus Dpb1":
                case "locus DPB1":
                    return new[] {Locus.Dpb1};
                case "locus Dqb1":
                case "locus DQB1":
                    return new[] {Locus.Dqb1};
                case "locus Drb1":
                case "locus DRB1":
                    return new[] {Locus.Drb1};
                default:
                    throw new ArgumentOutOfRangeException($"Locus: {locusType} not recognised");
            }
        }
    }
}