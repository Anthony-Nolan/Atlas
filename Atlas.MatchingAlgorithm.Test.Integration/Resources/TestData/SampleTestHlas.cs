﻿using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;

namespace Atlas.MatchingAlgorithm.Test.Integration.Resources.TestData
{
    /// <summary>
    /// Holds sets of HLA phenotypes that can be re-used across the integration test suite.
    /// Phenotypes in Set1 are mismatched at every position to those in Set2.
    /// </summary>
    public class SampleTestHlas
    {
        public class HeterozygousSet1 : ITestHlaSet
        {
            public PhenotypeInfo<string> ThreeLocus_SingleExpressingAlleles => new PhenotypeInfo<string>
            {
                A = new LocusInfo<string>("01:01:02", "02:01:01:02L"),
                B = new LocusInfo<string>("07:68:01", "08:01:01:01"),
                Drb1 = new LocusInfo<string>("01:01:01", "03:02:01")
            };

            public PhenotypeInfo<string> SixLocus_SingleExpressingAlleles => new PhenotypeInfoBuilder<string>(ThreeLocus_SingleExpressingAlleles)
                .WithDataAt(Locus.C, LocusPosition.One, "01:02:01:01")
                .WithDataAt(Locus.C, LocusPosition.Two, "02:02:01")
                .WithDataAt(Locus.Dpb1, LocusPosition.One, "01:01:01:01")
                .WithDataAt(Locus.Dpb1, LocusPosition.Two, "09:01:01")
                .WithDataAt(Locus.Dqb1, LocusPosition.One, "02:01:11")
                .WithDataAt(Locus.Dqb1, LocusPosition.Two, "03:01:01:01")
                .Build();

            public PhenotypeInfo<string> SixLocus_ExpressingAlleles_WithTruncatedNames => new PhenotypeInfo<string>
            {
                A = new LocusInfo<string>("01:01", "02:01"),
                B = new LocusInfo<string>("07:68", "08:01"),
                C = new LocusInfo<string>("01:02", "02:02"),
                Dpb1 = new LocusInfo<string>("01:01", "09:01"),
                Dqb1 = new LocusInfo<string>("02:01", "03:01"),
                Drb1 = new LocusInfo<string>("01:01", "03:02")
            };

            public PhenotypeInfo<string> SixLocus_XxCodes => new PhenotypeInfo<string>
            {
                A = new LocusInfo<string>("01:XX", "02:XX"),
                B = new LocusInfo<string>("07:XX", "08:XX"),
                C = new LocusInfo<string>("01:XX", "02:XX"),
                Dpb1 = new LocusInfo<string>("01:XX", "09:XX"),
                Dqb1 = new LocusInfo<string>("02:XX", "03:XX"),
                Drb1 = new LocusInfo<string>("01:XX", "03:XX")
            };

            public PhenotypeInfo<string> FiveLocus_Serologies => new PhenotypeInfo<string>
            {
                A = new LocusInfo<string>("1", "2"),
                B = new LocusInfo<string>("7", "8"),
                C = new LocusInfo<string>("1", "2"),
                Dqb1 = new LocusInfo<string>("2", "3"),
                Drb1 = new LocusInfo<string>("1", "3")
            };
        }

        public class HeterozygousSet2 : ITestHlaSet
        {
            public PhenotypeInfo<string> ThreeLocus_SingleExpressingAlleles => new PhenotypeInfo<string>
            {
                A = new LocusInfo<string>("03:02:03", "11:01:01:01"),
                B = new LocusInfo<string>("13:01:02", "14:06:01"),
                Drb1 = new LocusInfo<string>("04:05:01:01", "08:02:04")
            };

            public PhenotypeInfo<string> SixLocus_SingleExpressingAlleles => new PhenotypeInfoBuilder<string>(ThreeLocus_SingleExpressingAlleles)
                .WithDataAt(Locus.C, LocusPosition.One, "03:02:01")
                .WithDataAt(Locus.C, LocusPosition.Two, "04:42:01")
                .WithDataAt(Locus.Dpb1, LocusPosition.One, "39:01:01:04")
                .WithDataAt(Locus.Dpb1, LocusPosition.Two, "124:01:01:01")
                .WithDataAt(Locus.Dqb1, LocusPosition.One, "04:02:10")
                .WithDataAt(Locus.Dqb1, LocusPosition.Two, "05:01:01:05")
                .Build();

            public PhenotypeInfo<string> SixLocus_ExpressingAlleles_WithTruncatedNames => new PhenotypeInfo<string>
            {
                A = new LocusInfo<string>("03:02", "11:01"),
                B = new LocusInfo<string>("13:01", "14:06"),
                C = new LocusInfo<string>("03:02", "04:42"),
                Dpb1 = new LocusInfo<string>("39:01", "124:01"),
                Dqb1 = new LocusInfo<string>("04:02", "05:01"),
                Drb1 = new LocusInfo<string>("04:05", "08:02")
            };

            public PhenotypeInfo<string> SixLocus_XxCodes => new PhenotypeInfo<string>
            {
                A = new LocusInfo<string>("03:XX", "11:XX"),
                B = new LocusInfo<string>("13:XX", "14:XX"),
                C = new LocusInfo<string>("03:XX", "04:XX"),
                Dpb1 = new LocusInfo<string>("39:XX", "124:XX"),
                Dqb1 = new LocusInfo<string>("04:XX", "05:XX"),
                Drb1 = new LocusInfo<string>("04:XX", "08:XX")
            };

            public PhenotypeInfo<string> FiveLocus_Serologies => new PhenotypeInfo<string>
            {
                A = new LocusInfo<string>("3", "11"),
                B = new LocusInfo<string>("13", "14"),
                C = new LocusInfo<string>("3", "4"),
                Dqb1 = new LocusInfo<string>("4", "5"),
                Drb1 = new LocusInfo<string>("4", "8")
            };
        }
    }
}