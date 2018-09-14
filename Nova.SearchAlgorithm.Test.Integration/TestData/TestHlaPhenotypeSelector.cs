using Nova.SearchAlgorithm.Common.Models;
using System;

namespace Nova.SearchAlgorithm.Test.Integration.TestData
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
                case TestHlaPhenotypeCategory.FiveLocusSingleExpressingAlleles:
                    return testHlaSet.FiveLocus_SingleExpressingAlleles;
                case TestHlaPhenotypeCategory.FiveLocusExpressingAllelesWithTruncatedNames:
                    return testHlaSet.FiveLocus_ExpressingAlleles_WithTruncatedNames;
                case TestHlaPhenotypeCategory.FiveLocusXxCodes:
                    return testHlaSet.FiveLocus_XxCodes;
                case TestHlaPhenotypeCategory.FiveLocusSerologies:
                    return testHlaSet.FiveLocus_Serologies;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
