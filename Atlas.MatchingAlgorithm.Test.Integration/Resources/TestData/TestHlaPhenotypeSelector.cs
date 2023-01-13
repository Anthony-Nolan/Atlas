using System;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;

namespace Atlas.MatchingAlgorithm.Test.Integration.Resources.TestData
{
    public class TestHlaPhenotypeSelector
    {
        public static PhenotypeInfo<string> GetTestHlaPhenotype(
            ITestHlaSet testHlaSet, 
            TestHlaPhenotypeCategory category)
        {
            switch (category)
            {
                case TestHlaPhenotypeCategory.ThreeLocusSingleExpressingAlleles:
                    return testHlaSet.ThreeLocus_SingleExpressingAlleles;
                case TestHlaPhenotypeCategory.SixLocusSingleExpressingAlleles:
                    return testHlaSet.SixLocus_SingleExpressingAlleles;
                case TestHlaPhenotypeCategory.SixLocusExpressingAllelesWithTruncatedNames:
                    return testHlaSet.SixLocus_ExpressingAlleles_WithTruncatedNames;
                case TestHlaPhenotypeCategory.SixLocusXxCodes:
                    return testHlaSet.SixLocus_XxCodes;
                case TestHlaPhenotypeCategory.FiveLocusSerologies:
                    return testHlaSet.FiveLocus_Serologies;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
